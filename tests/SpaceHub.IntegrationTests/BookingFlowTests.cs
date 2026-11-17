using Microsoft.Extensions.DependencyInjection;
using SpaceHub.Application;
using SpaceHub.Infrastructure;
using SpaceHub.Application.Bookings;
using SpaceHub.Domain.Entities.Enums;
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

public sealed class BookingFlowTests
{
    [Fact]
    public async Task Bookings_price_snapshots_serialize_confirmations_and_enforce_lifecycle()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "SpaceHub.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        var api = Path.Combine(root.FullName, "src", "SpaceHub.Api");
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var dll = Path.Combine(api, "bin", configuration, "net10.0", "SpaceHub.Api.dll");
        var databaseName = "SpaceHub_BookingTest_" + Guid.NewGuid().ToString("N");
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
                var result = await Send(HttpMethod.Post, "/api/auth/register", new { email, password, fullName = "Booking User" }, 201);
                var id = result.GetProperty("user").GetProperty("id").GetInt32();
                var token = result.GetProperty("accessToken").GetString()!;
                if (owner) token = (await Send(HttpMethod.Post, "/api/me/roles/owner", null, 200, token)).GetProperty("accessToken").GetString()!;
                return (token, id);
            }
            var owner = await Account("owner@example.com", true);
            var customer = await Account("customer@example.com", false);
            var stranger = await Account("stranger@example.com", true);
            var space = await Send(HttpMethod.Post, "/api/spaces", new { name = "Booking Hall", description = "A hall for tests", address = "Example Street 10", city = "Tbilisi", capacity = 40, hourlyRate = 50m }, 201, owner.Token);
            var spaceId = space.GetProperty("id").GetInt32();
            async Task<string> Version() => (await Send(HttpMethod.Get, $"/api/spaces/{spaceId}", null, 200, owner.Token)).GetProperty("rowVersion").GetString()!;
            var start = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(7).AddHours(6), TimeSpan.Zero); // 10:00 Georgia.
            var end = start.AddHours(2);
            await Send(HttpMethod.Put, $"/api/spaces/{spaceId}/working-hours", new { days = Enumerable.Range(0,7).Select(d => new { dayOfWeek = d, opensAt = "08:00:00", closesAt = "22:00:00" }), rowVersion = await Version() }, 200, owner.Token);
            var projector = await Send(HttpMethod.Post, $"/api/spaces/{spaceId}/services", new { name = "Projector", pricingType = 0, unitPrice = 30m, spaceRowVersion = await Version() }, 201, owner.Token);
            var projectorId = projector.GetProperty("id").GetInt32();
            var food = await Send(HttpMethod.Post, $"/api/spaces/{spaceId}/services", new { name = "Food", pricingType = 1, unitPrice = 10m, spaceRowVersion = await Version() }, 201, owner.Token);
            var foodId = food.GetProperty("id").GetInt32();
            var package = await Send(HttpMethod.Post, $"/api/spaces/{spaceId}/packages", new { name = "Meeting", description = "Projector and food for 20", price = 100m,
                items = new[] { new { spaceServiceId = projectorId, quantity = 1 }, new { spaceServiceId = foodId, quantity = 20 } }, spaceRowVersion = await Version() }, 201, owner.Token);
            var packageId = package.GetProperty("id").GetInt32();
            object[] extras = [new { spaceServiceId = projectorId, quantity = (int?)2 }, new { spaceServiceId = foodId, quantity = (int?)null }];
            object Request(DateTimeOffset begins, DateTimeOffset ends, int attendees = 25, int? selectedPackage = null, object[]? selectedExtras = null)
                => new { spaceId, startsAt = begins, endsAt = ends, attendeeCount = attendees, packageId = selectedPackage, additionalServices = selectedExtras ?? [] };
            object Create(DateTimeOffset begins, DateTimeOffset ends, string version, int? selectedPackage = null, object[]? selectedExtras = null)
                => new { spaceId, startsAt = begins, endsAt = ends, attendeeCount = 25, packageId = selectedPackage, additionalServices = selectedExtras ?? [], spaceRowVersion = version, totalAmount = 0m, userId = stranger.Id };
            await Send(HttpMethod.Post, "/api/booking-quotes", Request(start,end), 401);
            await Send(HttpMethod.Post, "/api/booking-quotes", Request(start,end,attendees:41), 400, customer.Token);
            await Send(HttpMethod.Post, "/api/booking-quotes", Request(start.AddMinutes(15),end), 400, customer.Token);
            await Send(HttpMethod.Post, "/api/booking-quotes", Request(start,start.AddMinutes(30)), 400, customer.Token);
            await Send(HttpMethod.Post, "/api/booking-quotes", Request(start.Date.AddHours(1), start.Date.AddHours(2)), 400, customer.Token);
            await Send(HttpMethod.Post, "/api/booking-quotes", Request(start,end,selectedPackage:int.MaxValue), 400, customer.Token);
            await Send(HttpMethod.Post, "/api/booking-quotes", Request(start,end,selectedExtras:[new { spaceServiceId = foodId, quantity = 1 }]), 400, customer.Token);
            await Send(HttpMethod.Post, "/api/booking-quotes", Request(start,end,selectedExtras:[new { spaceServiceId = projectorId }]), 400, customer.Token);
            await Send(HttpMethod.Post, "/api/booking-quotes", Request(start,end,selectedExtras:[extras[0],extras[0]]), 400, customer.Token);
            var quote = await Send(HttpMethod.Post, "/api/booking-quotes", Request(start,end,selectedPackage:packageId,selectedExtras:extras), 200, customer.Token);
            Assert.Equal(100m, quote.GetProperty("spaceSubtotal").GetDecimal());
            Assert.Equal(110m, quote.GetProperty("servicesSubtotal").GetDecimal());
            Assert.Equal(310m, quote.GetProperty("totalAmount").GetDecimal());
            Assert.Equal(4, quote.GetProperty("services").GetArrayLength());
            var cheap = await Send(HttpMethod.Post, "/api/booking-quotes", Request(start,end,attendees:10,selectedPackage:packageId), 200, customer.Token);
            Assert.Equal(200m, cheap.GetProperty("totalAmount").GetDecimal());
            await Send(HttpMethod.Put, $"/api/spaces/{spaceId}/services/{projectorId}", new { name = "Projector", pricingType = 0, unitPrice = 40m, isActive = true, spaceRowVersion = await Version() }, 200, owner.Token);
            await Send(HttpMethod.Post, "/api/bookings", Create(start,end,quote.GetProperty("spaceRowVersion").GetString()!,packageId,extras), 409, customer.Token);
            quote = await Send(HttpMethod.Post, "/api/booking-quotes", Request(start,end,selectedPackage:packageId,selectedExtras:extras), 200, customer.Token);
            var first = await Send(HttpMethod.Post, "/api/bookings", Create(start,end,quote.GetProperty("spaceRowVersion").GetString()!,packageId,extras), 201, customer.Token);
            var firstId = first.GetProperty("id").GetInt32();
            Assert.Equal(customer.Id, first.GetProperty("userId").GetInt32());
            Assert.Equal(330m, first.GetProperty("totalAmount").GetDecimal());
            Assert.Equal(0, first.GetProperty("status").GetInt32());
            await Send(HttpMethod.Get, $"/api/bookings/{firstId}", null, 403, stranger.Token);
            await Send(HttpMethod.Get, $"/api/bookings/{firstId}", null, 200, owner.Token);
            await Send(HttpMethod.Get, "/api/me/received-bookings", null, 403, customer.Token);
            await Send(HttpMethod.Get, "/api/me/bookings?page=0", null, 400, customer.Token);
            await Send(HttpMethod.Post, $"/api/bookings/{firstId}/confirm", new { rowVersion = first.GetProperty("rowVersion").GetString() }, 403, stranger.Token);
            await Send(HttpMethod.Post, $"/api/bookings/{firstId}/confirm", new { rowVersion = first.GetProperty("rowVersion").GetString() }, 403, customer.Token);
            // Pending requests do not reserve the time: a second overlapping request is allowed.
            quote = await Send(HttpMethod.Post, "/api/booking-quotes", Request(start,end), 200, customer.Token);
            var second = await Send(HttpMethod.Post, "/api/bookings", Create(start,end,quote.GetProperty("spaceRowVersion").GetString()!), 201, customer.Token);
            var secondId = second.GetProperty("id").GetInt32();
            async Task<HttpResponseMessage> Confirm(JsonElement booking)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/bookings/{booking.GetProperty("id").GetInt32()}/confirm")
                    { Content = JsonContent.Create(new { rowVersion = booking.GetProperty("rowVersion").GetString() }) };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
                return await client.SendAsync(request);
            }
            var races = await Task.WhenAll(Confirm(first), Confirm(second));
            Assert.Equal(new[] {200,409}, races.Select(x => (int)x.StatusCode).Order().ToArray());
            foreach (var response in races) response.Dispose();
            first = await Send(HttpMethod.Get, $"/api/bookings/{firstId}", null, 200, customer.Token);
            second = await Send(HttpMethod.Get, $"/api/bookings/{secondId}", null, 200, customer.Token);
            Assert.Equal(new[] {1,2}, new[] {first.GetProperty("status").GetInt32(), second.GetProperty("status").GetInt32()}.Order().ToArray());
            Assert.EndsWith("Z", first.GetProperty("startsAt").GetString());
            var winner = first.GetProperty("status").GetInt32() == 1 ? first : second;
            var winnerId = winner.GetProperty("id").GetInt32();
            await Send(HttpMethod.Post, "/api/booking-quotes", Request(start,end), 409, customer.Token);
            var availabilityUrl = $"/api/spaces/{spaceId}/availability?startsAt={Uri.EscapeDataString(start.ToString("O"))}&endsAt={Uri.EscapeDataString(end.ToString("O"))}";
            Assert.False((await Send(HttpMethod.Get, availabilityUrl, null, 200, customer.Token)).GetProperty("isAvailable").GetBoolean());
            // Adjacent bookings are valid; customer can cancel a confirmed booking >24h ahead.
            quote = await Send(HttpMethod.Post, "/api/booking-quotes", Request(end,end.AddHours(1)), 200, customer.Token);
            var adjacent = await Send(HttpMethod.Post, "/api/bookings", Create(end,end.AddHours(1),quote.GetProperty("spaceRowVersion").GetString()!), 201, customer.Token);
            var adjacentId = adjacent.GetProperty("id").GetInt32();
            adjacent = await Send(HttpMethod.Post, $"/api/bookings/{adjacentId}/confirm", new { rowVersion = adjacent.GetProperty("rowVersion").GetString() }, 200, owner.Token);
            await Send(HttpMethod.Post, $"/api/bookings/{adjacentId}/cancel", new { rowVersion = adjacent.GetProperty("rowVersion").GetString() }, 200, customer.Token);
            await Send(HttpMethod.Post, $"/api/bookings/{adjacentId}/cancel", new { rowVersion = adjacent.GetProperty("rowVersion").GetString() }, 409, customer.Token);
            // Explicit rejection requires a reason and is visible in the customer's history.
            quote = await Send(HttpMethod.Post, "/api/booking-quotes", Request(start.AddDays(1),end.AddDays(1)), 200, customer.Token);
            var pending = await Send(HttpMethod.Post, "/api/bookings", Create(start.AddDays(1),end.AddDays(1),quote.GetProperty("spaceRowVersion").GetString()!), 201, customer.Token);
            var pendingId = pending.GetProperty("id").GetInt32();
            await Send(HttpMethod.Post, $"/api/bookings/{pendingId}/reject", new { reason = " ", rowVersion = pending.GetProperty("rowVersion").GetString() }, 400, owner.Token);
            var rejected = await Send(HttpMethod.Post, $"/api/bookings/{pendingId}/reject", new { reason = "Maintenance", rowVersion = pending.GetProperty("rowVersion").GetString() }, 200, owner.Token);
            Assert.Equal("Maintenance", rejected.GetProperty("decisionReason").GetString());
            // Cancellation propagates to the linked event and confirmed registrations.
            int eventId, nearId, expiredId, completedId;
            await using (var seed = new AppDbContext(options))
            {
                var ev = new Event { BookingId = winnerId, Title = "Test event", Description = "Test", StartsAt = start.UtcDateTime,
                    EndsAt = end.UtcDateTime, MaxAttendees = 25, Status = EventStatus.Published, CreatedAt = DateTime.UtcNow };
                ev.Registrations.Add(new Registration { UserId = customer.Id, RegisteredAt = DateTime.UtcNow, Status = RegistrationStatus.Confirmed });
                seed.EventRecords.Add(ev);
                Booking SeedBooking(DateTime begins, DateTime ends, BookingStatus status) => new() { SpaceId = spaceId, UserId = customer.Id, StartsAt = begins, EndsAt = ends,
                    AttendeeCount = 2, Status = status, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                var near = SeedBooking(DateTime.UtcNow.AddHours(3),DateTime.UtcNow.AddHours(4),BookingStatus.Confirmed);
                var expired = SeedBooking(DateTime.UtcNow.AddHours(-2),DateTime.UtcNow.AddHours(-1),BookingStatus.Pending);
                var completed = SeedBooking(DateTime.UtcNow.AddDays(-1),DateTime.UtcNow.AddDays(-1).AddHours(1),BookingStatus.Confirmed);
                seed.BookingRecords.AddRange(near,expired,completed); await seed.SaveChangesAsync();
                eventId = ev.Id; nearId=near.Id; expiredId=expired.Id; completedId=completed.Id;
            }
            var nearDto = await Send(HttpMethod.Get, $"/api/bookings/{nearId}", null, 200, customer.Token);
            await Send(HttpMethod.Post, $"/api/bookings/{nearId}/cancel", new { rowVersion = nearDto.GetProperty("rowVersion").GetString() }, 400, customer.Token);
            await Send(HttpMethod.Post, $"/api/bookings/{winnerId}/cancel", new { rowVersion = winner.GetProperty("rowVersion").GetString() }, 400, owner.Token);
            await Send(HttpMethod.Post, $"/api/bookings/{winnerId}/cancel", new { reason = "Venue maintenance", rowVersion = winner.GetProperty("rowVersion").GetString() }, 200, owner.Token);
            var services = new ServiceCollection(); services.AddLogging(); services.AddApplication(); services.AddInfrastructure(connection);
            await using (var provider = services.BuildServiceProvider())
            {
                await using var scope = provider.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IBookingService>().RefreshDueAsync(CancellationToken.None);
            }
            await using var verify = new AppDbContext(options);
            Assert.Equal(EventStatus.Cancelled, (await verify.EventRecords.SingleAsync(x=>x.Id==eventId)).Status);
            Assert.Equal(RegistrationStatus.Cancelled, (await verify.RegistrationRecords.SingleAsync(x=>x.EventId==eventId)).Status);
            Assert.Equal(BookingStatus.Rejected, (await verify.BookingRecords.SingleAsync(x=>x.Id==expiredId)).Status);
            Assert.Equal(BookingStatus.Completed, (await verify.BookingRecords.SingleAsync(x=>x.Id==completedId)).Status);
            var saved = await verify.BookingRecords.Include(x=>x.Services).SingleAsync(x=>x.Id==firstId);
            Assert.Equal(330m,saved.TotalAmount);
            Assert.Equal(4,saved.Services.Count);
            Assert.True((await Send(HttpMethod.Get,"/api/me/bookings?status=2&pageSize=1",null,200,customer.Token)).GetProperty("totalCount").GetInt32()>=2);
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
