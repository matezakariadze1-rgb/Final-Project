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

public sealed class SpaceFlowTests
{
    [Fact]
    public async Task Spaces_enforce_ownership_validation_visibility_concurrency_and_booking_constraints()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "SpaceHub.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        var api = Path.Combine(root.FullName, "src", "SpaceHub.Api");
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var dll = Path.Combine(api, "bin", configuration, "net10.0", "SpaceHub.Api.dll");
        var databaseName = "SpaceHub_SpaceTest_" + Guid.NewGuid().ToString("N");
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
            const string password = "CoursePass!2026";
            async Task<(string Token, int Id)> Account(string email, bool owner)
            {
                var result = await Send(HttpMethod.Post, "/api/auth/register", new { email, password, fullName = "Space Owner" }, 201);
                var id = result.GetProperty("user").GetProperty("id").GetInt32();
                var token = result.GetProperty("accessToken").GetString()!;
                if (owner) token = (await Send(HttpMethod.Post, "/api/me/roles/owner", null, 200, token)).GetProperty("accessToken").GetString()!;
                return (token, id);
            }
            var attendee = await Account("attendee@example.com", false);
            var owner = await Account("owner@example.com", true);
            var other = await Account("other@example.com", true);
            object Create(string name = "Studio Alpha", string city = "Tbilisi", decimal rate = 50m)
                => new { name, description = "A bright meeting room", address = "Example Street 10", city, capacity = 40, hourlyRate = rate, ownerId = other.Id };
            await Send(HttpMethod.Get, "/api/spaces", null, 401);
            await Send(HttpMethod.Post, "/api/spaces", Create(), 403, attendee.Token);
            var invalid = await Send(HttpMethod.Post, "/api/spaces", new { name = " ", description = "", address = "x", city = "x", capacity = 0, hourlyRate = -1 }, 400, owner.Token);
            Assert.True(invalid.GetProperty("errors").EnumerateObject().Count() >= 5);
            await Send(HttpMethod.Post, "/api/spaces", Create(rate: 1.234m), 400, owner.Token);
            var created = await Send(HttpMethod.Post, "/api/spaces", Create(), 201, owner.Token);
            var id = created.GetProperty("id").GetInt32();
            Assert.Equal(owner.Id, created.GetProperty("ownerId").GetInt32());
            Assert.Equal("Space Owner", created.GetProperty("ownerName").GetString());
            Assert.Equal("GEL", created.GetProperty("currency").GetString());
            Assert.False(created.TryGetProperty("owner", out _));
            string version = created.GetProperty("rowVersion").GetString()!;
            object Update(string rowVersion, int capacity = 40, decimal rate = 70m) => new
                { name = "Updated Studio", description = "A bright meeting room", address = "Example Street 10", city = "Tbilisi", capacity, hourlyRate = rate, rowVersion };
            await Send(HttpMethod.Put, $"/api/spaces/{id}", Update(version), 403, other.Token);
            await Send(HttpMethod.Put, $"/api/spaces/{id}/status", new { isActive = false, rowVersion = version }, 403, other.Token);
            await Send(HttpMethod.Get, "/api/spaces/2147483647", null, 404, owner.Token);
            await Send(HttpMethod.Get, "/api/me/spaces", null, 403, attendee.Token);
            await Send(HttpMethod.Put, $"/api/spaces/{id}", Update("AQ=="), 400, owner.Token);
            var updated = await Send(HttpMethod.Put, $"/api/spaces/{id}", Update(version), 200, owner.Token);
            await Send(HttpMethod.Put, $"/api/spaces/{id}", Update(version, rate: 99m), 409, owner.Token);
            version = updated.GetProperty("rowVersion").GetString()!;
            await Send(HttpMethod.Post, "/api/spaces", Create("Studio Beta", "Batumi", 20m), 201, owner.Token);
            await Send(HttpMethod.Post, "/api/spaces", Create("Studio Gamma"), 201, other.Token);
            var list = await Send(HttpMethod.Get, "/api/spaces?page=1&pageSize=1", null, 200, attendee.Token);
            Assert.Equal(3, list.GetProperty("totalCount").GetInt32());
            Assert.Equal(1, list.GetProperty("items").GetArrayLength());
            Assert.Equal(3, list.GetProperty("totalPages").GetInt32());
            var filtered = await Send(HttpMethod.Get, "/api/spaces?city=Batumi&minCapacity=30&maxHourlyRate=25&search=Beta", null, 200, attendee.Token);
            Assert.Equal(1, filtered.GetProperty("totalCount").GetInt32());
            var mine = await Send(HttpMethod.Get, "/api/me/spaces", null, 200, owner.Token);
            Assert.Equal(2, mine.GetProperty("totalCount").GetInt32());
            await Send(HttpMethod.Get, "/api/spaces?page=0&pageSize=999", null, 400, attendee.Token);
            await Send(HttpMethod.Get, "/api/spaces?maxHourlyRate=-1", null, 400, attendee.Token);
            // Omitted bool must not accidentally deactivate a space.
            await Send(HttpMethod.Put, $"/api/spaces/{id}/status", new { rowVersion = version }, 400, owner.Token);
            var hidden = await Send(HttpMethod.Put, $"/api/spaces/{id}/status", new { isActive = false, rowVersion = version }, 200, owner.Token);
            version = hidden.GetProperty("rowVersion").GetString()!;
            await Send(HttpMethod.Get, $"/api/spaces/{id}", null, 404, other.Token);
            await Send(HttpMethod.Get, $"/api/spaces/{id}/working-hours", null, 404, attendee.Token);
            await Send(HttpMethod.Get, $"/api/spaces/{id}", null, 200, owner.Token);
            Assert.Equal(2, (await Send(HttpMethod.Get, "/api/spaces", null, 200, attendee.Token)).GetProperty("totalCount").GetInt32());
            Assert.Equal(2, (await Send(HttpMethod.Get, "/api/me/spaces", null, 200, owner.Token)).GetProperty("totalCount").GetInt32());
            var active = await Send(HttpMethod.Put, $"/api/spaces/{id}/status", new { isActive = true, rowVersion = version }, 200, owner.Token);
            version = active.GetProperty("rowVersion").GetString()!;
            var starts = DateTime.UtcNow.Date.AddDays(7).AddHours(10); // 14:00 in Georgia.
            var day = (int)starts.DayOfWeek;
            object Hours(string rowVersion, string opens = "09:00:00", string closes = "18:00:00")
                => new { days = new[] { new { dayOfWeek = day, opensAt = opens, closesAt = closes } }, rowVersion };
            await Send(HttpMethod.Put, $"/api/spaces/{id}/working-hours", Hours(version), 403, other.Token);
            await Send(HttpMethod.Put, $"/api/spaces/{id}/working-hours", Hours(version, closes: "08:00:00"), 400, owner.Token);
            await Send(HttpMethod.Put, $"/api/spaces/{id}/working-hours", Hours(version, opens: "09:15:00"), 400, owner.Token);
            await Send(HttpMethod.Put, $"/api/spaces/{id}/working-hours", new { days = new object?[] { null }, rowVersion = version }, 400, owner.Token);
            var duplicateDays = new[] { new { dayOfWeek = day, opensAt = "09:00:00", closesAt = "18:00:00" }, new { dayOfWeek = day, opensAt = "10:00:00", closesAt = "19:00:00" } };
            await Send(HttpMethod.Put, $"/api/spaces/{id}/working-hours", new { days = duplicateDays, rowVersion = version }, 400, owner.Token);
            var hours = await Send(HttpMethod.Put, $"/api/spaces/{id}/working-hours", Hours(version), 200, owner.Token);
            Assert.Equal("Asia/Tbilisi", hours.GetProperty("timeZone").GetString());
            Assert.Equal(1, hours.GetProperty("days").GetArrayLength());
            await Send(HttpMethod.Put, $"/api/spaces/{id}/working-hours", Hours(version, opens: "10:00:00"), 409, owner.Token);
            version = hours.GetProperty("rowVersion").GetString()!;
            Assert.Equal(1, (await Send(HttpMethod.Get, $"/api/spaces/{id}", null, 200, attendee.Token)).GetProperty("workingDays").GetInt32());
            // Seed an upcoming booking to verify edits cannot invalidate existing commitments.
            int bookingId;
            await using (var seed = new AppDbContext(options))
            {
                var booking = new Booking { UserId = attendee.Id, SpaceId = id, StartsAt = starts, EndsAt = starts.AddHours(1),
                    AttendeeCount = 30, Status = SpaceHub.Domain.Entities.Enums.BookingStatus.Confirmed, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
                    HourlyRateSnapshot = 70m, SpaceSubtotal = 70m, TotalAmount = 70m };
                seed.BookingRecords.Add(booking); await seed.SaveChangesAsync(); bookingId = booking.Id;
            }
            await Send(HttpMethod.Put, $"/api/spaces/{id}", Update(version, capacity: 20), 400, owner.Token);
            await Send(HttpMethod.Put, $"/api/spaces/{id}/working-hours", Hours(version, closes: "13:00:00"), 400, owner.Token);
            hours = await Send(HttpMethod.Put, $"/api/spaces/{id}/working-hours", Hours(version, opens: "10:00:00"), 200, owner.Token);
            version = hours.GetProperty("rowVersion").GetString()!;
            updated = await Send(HttpMethod.Put, $"/api/spaces/{id}", Update(version, rate: 90m), 200, owner.Token);
            version = updated.GetProperty("rowVersion").GetString()!;
            await Send(HttpMethod.Put, $"/api/spaces/{id}/status", new { isActive = false, rowVersion = version }, 200, owner.Token);
            await using var verify = new AppDbContext(options);
            var savedBooking = await verify.BookingRecords.SingleAsync(x => x.Id == bookingId);
            Assert.Equal(70m, savedBooking.HourlyRateSnapshot);
            Assert.Equal(SpaceHub.Domain.Entities.Enums.BookingStatus.Confirmed, savedBooking.Status);
            Assert.Equal(new TimeOnly(10, 0), (await verify.SpaceWorkingHoursRecords.SingleAsync(x => x.SpaceId == id)).OpensAt);
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
