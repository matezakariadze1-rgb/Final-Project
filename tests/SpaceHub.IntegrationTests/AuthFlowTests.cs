using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SpaceHub.Domain.Entities;
using SpaceHub.Infrastructure.Data;
using Xunit;

namespace SpaceHub.IntegrationTests;

public sealed class AuthFlowTests
{
    [Fact]
    public async Task Authentication_flow_preserves_roles_and_rejects_invalid_requests_and_tokens()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "SpaceHub.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        var api = Path.Combine(root.FullName, "src", "SpaceHub.Api");
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var dll = Path.Combine(api, "bin", configuration, "net10.0", "SpaceHub.Api.dll");
        var databaseName = "SpaceHub_AuthTest_" + Guid.NewGuid().ToString("N");
        var dbFile = Path.Combine(Path.GetTempPath(), databaseName + ".mdf");
        var connection = $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};AttachDbFilename={dbFile};Trusted_Connection=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options;
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo("dotnet") { WorkingDirectory = api, UseShellExecute = false, CreateNoWindow = true };
        process.StartInfo.ArgumentList.Add(dll);
        process.StartInfo.ArgumentList.Add("--urls");
        process.StartInfo.ArgumentList.Add($"http://127.0.0.1:{port}");
        process.StartInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        process.StartInfo.Environment["ConnectionStrings__DefaultConnection"] = connection;
        process.StartInfo.Environment["Jwt__Key"] = key;
        process.StartInfo.Environment["Jwt__Issuer"] = "SpaceHub.Test";
        process.StartInfo.Environment["Jwt__Audience"] = "SpaceHub.Test.Client";
        process.StartInfo.Environment["Logging__LogLevel__Default"] = "Warning";
        var started = false;
        try
        {
            await using (var db = new AppDbContext(options)) await db.Database.MigrateAsync();
            Assert.True(process.Start()); started = true;
            using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}"), Timeout = TimeSpan.FromSeconds(15) };
            var ready = false;
            for (var attempt = 0; attempt < 50; attempt++)
            {
                try { ready = (await client.GetAsync("/api/health")).IsSuccessStatusCode; if (ready) break; }
                catch (HttpRequestException) { }
                await Task.Delay(200);
            }
            Assert.True(ready, "API startup failed.");
            // Frontend routing must not turn unknown API endpoints into HTML.
            using (var missingApi = await client.GetAsync("/api/frontend-route-does-not-exist"))
            {
                Assert.Equal(HttpStatusCode.NotFound, missingApi.StatusCode);
                Assert.Equal("application/json", missingApi.Content.Headers.ContentType?.MediaType);
            }
            using (var home = await client.GetAsync("/"))
            {
                Assert.Equal("/web/", home.RequestMessage!.RequestUri!.AbsolutePath);
                var content = await home.Content.ReadAsStringAsync();
                if (File.Exists(Path.Combine(api, "wwwroot", "web", "index.html")))
                {
                    Assert.Equal(HttpStatusCode.OK, home.StatusCode);
                    Assert.Equal("text/html", home.Content.Headers.ContentType?.MediaType);
                    Assert.Contains("lang=\"ka\"", content);
                    var assets = System.Text.RegularExpressions.Regex.Matches(content, "(?:src|href)=\"(/web/assets/[^\"]+)\"");
                    Assert.True(assets.Count >= 2, "Built HTML should reference JS and CSS assets.");
                    foreach (System.Text.RegularExpressions.Match asset in assets)
                    {
                        using var response = await client.GetAsync(asset.Groups[1].Value);
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.NotEqual("text/html", response.Content.Headers.ContentType?.MediaType);
                    }
                }
                else
                {
                    Assert.Equal(HttpStatusCode.NotFound, home.StatusCode);
                    Assert.Contains("Frontend is not built", content);
                }
            }

            async Task<JsonElement> Send(HttpMethod method, string path, object? body, int status, string? token = null)
            {
                using var request = new HttpRequestMessage(method, path);
                if (body is not null) request.Content = JsonContent.Create(body);
                if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var response = await client.SendAsync(request);
                Assert.Equal(status, (int)response.StatusCode);
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (status >= 400) { Assert.Equal(status, json.GetProperty("status").GetInt32()); Assert.True(json.TryGetProperty("errors", out _)); }
                return json;
            }
            const string email = "student@example.com";
            const string password = "CoursePass!2026";
            var invalid = await Send(HttpMethod.Post, "/api/auth/register", new { email = "bad", password = "x", fullName = " " }, 400);
            Assert.True(invalid.GetProperty("errors").EnumerateObject().Count() >= 3);
            await Send(HttpMethod.Post, "/api/auth/register", new { email = (string?)null, password = (string?)null, fullName = (string?)null }, 400);
            await Send(HttpMethod.Get, "/api/me", null, 401);
            var registered = await Send(HttpMethod.Post, "/api/auth/register", new { email = "Student@Example.com", password, fullName = "Test Student", role = "Owner" }, 201);
            var user = registered.GetProperty("user");
            Assert.Equal(email, user.GetProperty("email").GetString());
            Assert.Equal("Attendee", user.GetProperty("roles")[0].GetString());
            Assert.Equal(1, user.GetProperty("roles").GetArrayLength());
            Assert.False(user.TryGetProperty("passwordHash", out _));
            var token = registered.GetProperty("accessToken").GetString()!;
            Assert.True(registered.GetProperty("expiresAt").GetDateTime() > DateTime.UtcNow);
            var claims = new JwtSecurityTokenHandler().ReadJwtToken(token);
            Assert.Contains(claims.Claims, x => x.Type == "sub" && x.Value == user.GetProperty("id").GetInt32().ToString());
            Assert.Contains(claims.Claims, x => x.Type == "email" && x.Value == email);
            await Send(HttpMethod.Get, "/api/me", null, 200, token);
            await Send(HttpMethod.Post, "/api/auth/register", new { email = "STUDENT@example.com", password, fullName = "Other Name" }, 409);
            await Send(HttpMethod.Post, "/api/auth/login", new { email, password = "wrong-password" }, 401);
            await Send(HttpMethod.Post, "/api/auth/login", new { email = "missing@example.com", password }, 401);
            await Send(HttpMethod.Post, "/api/auth/login", new { email = "STUDENT@example.com", password }, 200);
            await Send(HttpMethod.Post, "/api/me/roles/admin", null, 400, token);
            var owner = await Send(HttpMethod.Post, "/api/me/roles/owner", null, 200, token);
            token = owner.GetProperty("accessToken").GetString()!;
            await Send(HttpMethod.Post, "/api/me/roles/owner", null, 200, token);
            var organizer = await Send(HttpMethod.Post, "/api/me/roles/organizer", null, 200, token);
            token = organizer.GetProperty("accessToken").GetString()!;
            Assert.Equal(3, organizer.GetProperty("user").GetProperty("roles").GetArrayLength());
            claims = new JwtSecurityTokenHandler().ReadJwtToken(token);
            Assert.Equal(3, claims.Claims.Count(x => x.Type == "role"));
            await Send(HttpMethod.Get, "/api/me", null, 401, token[..^8] + "abcdefgh");
            string Signed(string issuer, string audience, DateTime expires, string subject = "1")
            {
                var jwt = new JwtSecurityToken(issuer, audience, [new Claim("sub", subject)],
                    DateTime.UtcNow.AddHours(-2), expires, new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
                return new JwtSecurityTokenHandler().WriteToken(jwt);
            }
            await Send(HttpMethod.Get, "/api/me", null, 401, Signed("SpaceHub.Test", "SpaceHub.Test.Client", DateTime.UtcNow.AddMinutes(-1)));
            await Send(HttpMethod.Get, "/api/me", null, 401, Signed("Wrong", "SpaceHub.Test.Client", DateTime.UtcNow.AddMinutes(5)));
            await Send(HttpMethod.Get, "/api/me", null, 401, Signed("SpaceHub.Test", "Wrong", DateTime.UtcNow.AddMinutes(5)));
            await Send(HttpMethod.Get, "/api/me", null, 401, Signed("SpaceHub.Test", "SpaceHub.Test.Client", DateTime.UtcNow.AddMinutes(5), "invalid"));
            // Exercise uniqueness with concurrent HTTP requests, not only a pre-insert lookup.
            var attempts = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => client.PostAsJsonAsync("/api/auth/register",
                new { email = "race@example.com", password, fullName = "Concurrent Student" })));
            Assert.Equal(new[] { 201, 409 }, attempts.Select(x => (int)x.StatusCode).Order().ToArray());
            foreach (var response in attempts) response.Dispose();
            var swagger = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
            Assert.Equal("bearer", swagger.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer").GetProperty("scheme").GetString());
            await using var verifyDb = new AppDbContext(options);
            var stored = await verifyDb.UserRecords.SingleAsync(x => x.Email == email);
            Assert.NotEqual(password, stored.PasswordHash);
            Assert.NotEqual(PasswordVerificationResult.Failed, new PasswordHasher<User>().VerifyHashedPassword(stored, stored.PasswordHash, password));
            Assert.Equal(3, await verifyDb.UserRoleRecords.CountAsync(x => x.UserId == stored.Id));
        }
        finally
        {
            if (started && !process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); }
            // This connection is exclusively the random database created by this test.
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
