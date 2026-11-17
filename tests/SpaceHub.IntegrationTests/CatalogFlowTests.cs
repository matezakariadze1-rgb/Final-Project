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

public sealed class CatalogFlowTests
{
    [Fact]
    public async Task Catalog_enforces_ownership_composition_visibility_snapshots_and_atomic_updates()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "SpaceHub.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        var api = Path.Combine(root.FullName, "src", "SpaceHub.Api");
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var dll = Path.Combine(api, "bin", configuration, "net10.0", "SpaceHub.Api.dll");
        var databaseName = "SpaceHub_CatalogTest_" + Guid.NewGuid().ToString("N");
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
                var result = await Send(HttpMethod.Post, "/api/auth/register", new { email, password, fullName = "Catalog Owner" }, 201);
                var id = result.GetProperty("user").GetProperty("id").GetInt32();
                var token = result.GetProperty("accessToken").GetString()!;
                if (owner) token = (await Send(HttpMethod.Post, "/api/me/roles/owner", null, 200, token)).GetProperty("accessToken").GetString()!;
                return (token, id);
            }
            var owner = await Account("owner@example.com", true);
            var other = await Account("other@example.com", true);
            var attendee = await Account("attendee@example.com", false);
            async Task<int> CreateSpace(string token) => (await Send(HttpMethod.Post, "/api/spaces",
                new { name = "Event Hall", description = "A hall for events", address = "Example Street 10", city = "Tbilisi", capacity = 40, hourlyRate = 50m }, 201, token)).GetProperty("id").GetInt32();
            var id = await CreateSpace(owner.Token);
            var foreignId = await CreateSpace(other.Token);
            var servicesPath = $"/api/spaces/{id}/services";
            var packagesPath = $"/api/spaces/{id}/packages";
            async Task<string> Version(int spaceId, string token) => (await Send(HttpMethod.Get, $"/api/spaces/{spaceId}", null, 200, token)).GetProperty("rowVersion").GetString()!;
            object Service(string version, string name = "Projector", int pricingType = 0, decimal price = 30m)
                => new { name, pricingType, unitPrice = price, spaceRowVersion = version };
            object ServiceUpdate(string version, bool active, int pricingType = 0, decimal price = 30m)
                => new { name = "Updated Projector", pricingType, unitPrice = price, isActive = active, spaceRowVersion = version };
            object Package(string version, object[] items, decimal price = 100m)
                => new { name = "Standard", description = "Equipment and catering", price, items, spaceRowVersion = version };
            object PackageUpdate(string version, object[] items, bool active, decimal price = 100m)
                => new { name = "Standard", description = "Equipment and catering", price, items, isActive = active, spaceRowVersion = version };
            await Send(HttpMethod.Get, servicesPath, null, 401);
            var initialVersion = await Version(id, owner.Token);
            await Send(HttpMethod.Post, servicesPath, Service(initialVersion), 403, attendee.Token);
            await Send(HttpMethod.Post, servicesPath, Service(initialVersion), 403, other.Token);
            var invalid = await Send(HttpMethod.Post, servicesPath, Service(initialVersion, name: " ", pricingType: 9, price: -1), 400, owner.Token);
            Assert.True(invalid.GetProperty("errors").EnumerateObject().Count() >= 3);
            await Send(HttpMethod.Post, servicesPath, Service(initialVersion, price: 1.234m), 400, owner.Token);
            var first = await Send(HttpMethod.Post, servicesPath, Service(initialVersion), 201, owner.Token);
            var firstId = first.GetProperty("id").GetInt32();
            Assert.Equal(id, first.GetProperty("spaceId").GetInt32());
            Assert.Equal("GEL", first.GetProperty("currency").GetString());
            Assert.False(first.TryGetProperty("space", out _));
            await Send(HttpMethod.Post, servicesPath, Service(initialVersion, name: "Must Roll Back"), 409, owner.Token);
            Assert.Equal(1, (await Send(HttpMethod.Get, servicesPath, null, 200, owner.Token)).GetProperty("totalCount").GetInt32());
            var second = await Send(HttpMethod.Post, servicesPath, Service(await Version(id, owner.Token), "Catering", 1, 10m), 201, owner.Token);
            var secondId = second.GetProperty("id").GetInt32();
            var foreignService = await Send(HttpMethod.Post, $"/api/spaces/{foreignId}/services", Service(await Version(foreignId, other.Token)), 201, other.Token);
            var foreignServiceId = foreignService.GetProperty("id").GetInt32();
            var v = await Version(id, owner.Token);
            object[] items = [new { spaceServiceId = firstId, quantity = 1 }, new { spaceServiceId = secondId, quantity = 20 }];
            await Send(HttpMethod.Get, $"{servicesPath}/{foreignServiceId}", null, 404, owner.Token);
            await Send(HttpMethod.Put, $"{servicesPath}/{firstId}", ServiceUpdate(v, true), 403, other.Token);
            await Send(HttpMethod.Post, packagesPath, Package(v, items), 403, other.Token);
            await Send(HttpMethod.Post, packagesPath, Package(v, []), 400, owner.Token);
            await Send(HttpMethod.Post, packagesPath, Package(v, [null!]), 400, owner.Token);
            await Send(HttpMethod.Post, packagesPath, Package(v, [new { spaceServiceId = firstId, quantity = 0 }]), 400, owner.Token);
            await Send(HttpMethod.Post, packagesPath, Package(v, [items[0], items[0]]), 400, owner.Token);
            await Send(HttpMethod.Post, packagesPath, Package(v, [new { spaceServiceId = foreignServiceId, quantity = 1 }]), 400, owner.Token);
            await Send(HttpMethod.Post, packagesPath, Package(v, [new { spaceServiceId = int.MaxValue, quantity = 1 }]), 400, owner.Token);
            var package = await Send(HttpMethod.Post, packagesPath, Package(v, items), 201, owner.Token);
            var packageId = package.GetProperty("id").GetInt32();
            Assert.Equal(2, package.GetProperty("items").GetArrayLength());
            Assert.Equal("Projector", package.GetProperty("items")[0].GetProperty("serviceName").GetString());
            Assert.Equal(100m, package.GetProperty("price").GetDecimal());
            Assert.Equal(1, (await Send(HttpMethod.Get, servicesPath + "?search=Catering&pageSize=1", null, 200, attendee.Token)).GetProperty("totalCount").GetInt32());
            Assert.Equal(1, (await Send(HttpMethod.Get, packagesPath + "?search=Standard&pageSize=1", null, 200, attendee.Token)).GetProperty("totalCount").GetInt32());
            await Send(HttpMethod.Get, servicesPath + "?page=0", null, 400, owner.Token);
            await Send(HttpMethod.Get, packagesPath + "?pageSize=101", null, 400, owner.Token);
            await Send(HttpMethod.Get, $"/api/spaces/{foreignId}/packages/{packageId}", null, 404, other.Token);
            v = await Version(id, owner.Token);
            await Send(HttpMethod.Put, $"{servicesPath}/{firstId}", ServiceUpdate(v, false), 400, owner.Token);
            await Send(HttpMethod.Put, $"{servicesPath}/{firstId}", ServiceUpdate(v, true, pricingType: 1), 400, owner.Token);
            await Send(HttpMethod.Put, $"{packagesPath}/{packageId}", PackageUpdate(v, items, false), 403, other.Token);
            await Send(HttpMethod.Put, $"{packagesPath}/{packageId}", PackageUpdate(v, items, false), 200, owner.Token);
            await Send(HttpMethod.Get, $"{packagesPath}/{packageId}", null, 404, attendee.Token);
            Assert.Equal(0, (await Send(HttpMethod.Get, packagesPath, null, 200, attendee.Token)).GetProperty("totalCount").GetInt32());
            Assert.Equal(1, (await Send(HttpMethod.Get, packagesPath, null, 200, owner.Token)).GetProperty("totalCount").GetInt32());
            v = await Version(id, owner.Token);
            await Send(HttpMethod.Put, $"{servicesPath}/{firstId}", ServiceUpdate(v, false), 200, owner.Token);
            await Send(HttpMethod.Get, $"{servicesPath}/{firstId}", null, 404, attendee.Token);
            await Send(HttpMethod.Get, $"{servicesPath}/{firstId}", null, 200, owner.Token);
            Assert.Equal(1, (await Send(HttpMethod.Get, servicesPath, null, 200, attendee.Token)).GetProperty("totalCount").GetInt32());
            v = await Version(id, owner.Token);
            await Send(HttpMethod.Put, $"{packagesPath}/{packageId}", PackageUpdate(v, items, true), 400, owner.Token);
            await Send(HttpMethod.Post, packagesPath, Package(v, [items[0]]), 400, owner.Token);
            await Send(HttpMethod.Put, $"{servicesPath}/{firstId}", ServiceUpdate(v, true), 200, owner.Token);
            v = await Version(id, owner.Token);
            await Send(HttpMethod.Put, $"{packagesPath}/{packageId}", PackageUpdate(v, items, true), 200, owner.Token);
            // A historical booking must remain independent of mutable catalog prices/items.
            int bookingId;
            await using (var seed = new AppDbContext(options))
            {
                var booking = new Booking { UserId = attendee.Id, SpaceId = id, PackageId = packageId, StartsAt = DateTime.UtcNow.AddDays(7), EndsAt = DateTime.UtcNow.AddDays(7).AddHours(2),
                    AttendeeCount = 20, Status = SpaceHub.Domain.Entities.Enums.BookingStatus.Confirmed, HourlyRateSnapshot = 50m, SpaceSubtotal = 100m,
                    PackageNameSnapshot = "Standard", PackagePriceSnapshot = 100m, TotalAmount = 200m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                booking.Services.Add(new BookingService { Booking = booking, SpaceServiceId = firstId, NameSnapshot = "Projector", Quantity = 1,
                    PricingTypeSnapshot = SpaceHub.Domain.Entities.Enums.ServicePricingType.PerUnit, UnitPriceSnapshot = 30m, Subtotal = 0m, IsIncludedInPackage = true });
                seed.BookingRecords.Add(booking); await seed.SaveChangesAsync(); bookingId = booking.Id;
            }
            v = await Version(id, owner.Token);
            await Send(HttpMethod.Put, $"{servicesPath}/{firstId}", ServiceUpdate(v, true, price: 80m), 200, owner.Token);
            v = await Version(id, owner.Token);
            var changed = await Send(HttpMethod.Put, $"{packagesPath}/{packageId}", PackageUpdate(v, [new { spaceServiceId = secondId, quantity = 30 }], true, 250m), 200, owner.Token);
            Assert.Equal(1, changed.GetProperty("items").GetArrayLength());
            Assert.Equal(30, changed.GetProperty("items")[0].GetProperty("quantity").GetInt32());
            await Send(HttpMethod.Put, $"{packagesPath}/{packageId}", PackageUpdate(v, items, true), 409, owner.Token);
            v = await Version(id, owner.Token);
            await Send(HttpMethod.Put, $"{packagesPath}/{packageId}", PackageUpdate(v, items, true, 250m), 200, owner.Token);
            // Concurrent creates sharing a space version: exactly one commits.
            v = await Version(id, owner.Token);
            async Task<HttpResponseMessage> ConcurrentCreate(string name)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, servicesPath) { Content = JsonContent.Create(Service(v, name)) };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
                return await client.SendAsync(request);
            }
            var race = await Task.WhenAll(ConcurrentCreate("Race A"), ConcurrentCreate("Race B"));
            Assert.Equal(new[] { 201, 409 }, race.Select(x => (int)x.StatusCode).Order().ToArray());
            foreach (var response in race) response.Dispose();
            Assert.Equal(1, (await Send(HttpMethod.Get, servicesPath + "?search=Race", null, 200, owner.Token)).GetProperty("totalCount").GetInt32());
            v = await Version(id, owner.Token);
            await Send(HttpMethod.Put, $"/api/spaces/{id}/status", new { isActive = false, rowVersion = v }, 200, owner.Token);
            await Send(HttpMethod.Get, servicesPath, null, 404, attendee.Token);
            await Send(HttpMethod.Get, packagesPath, null, 404, other.Token);
            await Send(HttpMethod.Get, servicesPath, null, 200, owner.Token);
            await using var verify = new AppDbContext(options);
            var saved = await verify.BookingRecords.Include(x => x.Services).SingleAsync(x => x.Id == bookingId);
            Assert.Equal(100m, saved.PackagePriceSnapshot);
            Assert.Equal(200m, saved.TotalAmount);
            Assert.Equal("Projector", saved.Services.Single().NameSnapshot);
            Assert.Equal(30m, saved.Services.Single().UnitPriceSnapshot);
            Assert.Equal(2, await verify.PackageItemRecords.CountAsync(x => x.PackageId == packageId));
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
