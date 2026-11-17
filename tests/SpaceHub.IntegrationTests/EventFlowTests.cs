using Microsoft.Extensions.DependencyInjection;
using SpaceHub.Application;
using SpaceHub.Infrastructure;
using SpaceHub.Application.Events;
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

public sealed class EventFlowTests
{
    [Fact]
    public async Task Events_require_owned_confirmed_bookings_and_preserve_private_access_and_lifecycle()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "SpaceHub.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        var api = Path.Combine(root.FullName, "src", "SpaceHub.Api");
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var dll = Path.Combine(api, "bin", configuration, "net10.0", "SpaceHub.Api.dll");
        var databaseName = "SpaceHub_EventTest_" + Guid.NewGuid().ToString("N");
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
            async Task<(string Token, int Id)> Account(string email,string? role)
            {
                var result=await Send(HttpMethod.Post,"/api/auth/register",new{email,password,fullName="Event User"},201);
                var id=result.GetProperty("user").GetProperty("id").GetInt32();var token=result.GetProperty("accessToken").GetString()!;
                if(role is not null)token=(await Send(HttpMethod.Post,$"/api/me/roles/{role}",null,200,token)).GetProperty("accessToken").GetString()!;
                return(token,id);
            }
            var owner=await Account("owner@example.com","owner");
            var organizer=await Account("organizer@example.com","organizer");
            var stranger=await Account("stranger@example.com","organizer");
            var attendee=await Account("attendee@example.com",null);
            await Send(HttpMethod.Get,"/api/me/organizer-profile",null,403,attendee.Token);
            await Send(HttpMethod.Get,"/api/me/organizer-profile",null,404,organizer.Token);
            await Send(HttpMethod.Put,"/api/me/organizer-profile",new{companyName=" ",phone="bad",website="file:///secret",description=""},400,organizer.Token);
            var space=await Send(HttpMethod.Post,"/api/spaces",new{name="Event Venue",description="Event test venue",address="Example Street 10",city="Tbilisi",capacity=40,hourlyRate=50},201,owner.Token);
            var spaceId=space.GetProperty("id").GetInt32();
            await Send(HttpMethod.Put,$"/api/spaces/{spaceId}/working-hours",new{days=Enumerable.Range(0,7).Select(d=>new{dayOfWeek=d,opensAt="08:00:00",closesAt="22:00:00"}),rowVersion=space.GetProperty("rowVersion").GetString()},200,owner.Token);
            var baseStart=new DateTimeOffset(DateTime.UtcNow.Date.AddDays(7).AddHours(6),TimeSpan.Zero);
            async Task<JsonElement> Booking(int offset,bool confirm=true)
            {
                var startsAt=baseStart.AddDays(offset);var endsAt=startsAt.AddHours(4);
                var quote=await Send(HttpMethod.Post,"/api/booking-quotes",new{spaceId,startsAt,endsAt,attendeeCount=25,packageId=(int?)null,additionalServices=Array.Empty<object>()},200,organizer.Token);
                var booking=await Send(HttpMethod.Post,"/api/bookings",new{spaceId,startsAt,endsAt,attendeeCount=25,packageId=(int?)null,additionalServices=Array.Empty<object>(),spaceRowVersion=quote.GetProperty("spaceRowVersion").GetString()},201,organizer.Token);
                if(confirm)booking=await Send(HttpMethod.Post,$"/api/bookings/{booking.GetProperty("id").GetInt32()}/confirm",new{rowVersion=booking.GetProperty("rowVersion").GetString()},200,owner.Token);
                return booking;
            }
            var booking=await Booking(0);var bookingId=booking.GetProperty("id").GetInt32();
            object Create(int bid,int day=0,int visibility=0,int[]?tags=null,int max=20)=>new{bookingId=bid,title="Workshop Event",description="A practical programming workshop",startsAt=baseStart.AddDays(day).AddHours(1),endsAt=baseStart.AddDays(day).AddHours(3),maxAttendees=max,ticketPrice=0m,visibility,tagIds=tags??[]};
            await Send(HttpMethod.Post,"/api/events",Create(bookingId),403,owner.Token);
            await Send(HttpMethod.Post,"/api/events",Create(bookingId),403,stranger.Token);
            await Send(HttpMethod.Post,"/api/events",Create(bookingId),400,organizer.Token);
            var profile=await Send(HttpMethod.Put,"/api/me/organizer-profile",new{companyName="Learning Group",phone="+995555123456",website="https://example.com",description="Workshops",isVerified=true,userId=stranger.Id},200,organizer.Token);
            Assert.Equal(organizer.Id,profile.GetProperty("userId").GetInt32());Assert.False(profile.GetProperty("isVerified").GetBoolean());
            var again=await Send(HttpMethod.Put,"/api/me/organizer-profile",new{companyName="Learning Group",phone="555123456",website="",description="Updated"},200,organizer.Token);
            Assert.Equal(profile.GetProperty("id").GetInt32(),again.GetProperty("id").GetInt32());
            await Send(HttpMethod.Post,"/api/tags",new{name="Course"},403,attendee.Token);
            var tag=await Send(HttpMethod.Post,"/api/tags",new{name="Course"},201,organizer.Token);var tagId=tag.GetProperty("id").GetInt32();
            await Send(HttpMethod.Post,"/api/tags",new{name="Course"},409,organizer.Token);
            Assert.Equal(1,(await Send(HttpMethod.Get,"/api/tags?search=Course&pageSize=1",null,200,attendee.Token)).GetProperty("totalCount").GetInt32());
            var pending=await Booking(4,false);
            await Send(HttpMethod.Post,"/api/events",Create(pending.GetProperty("id").GetInt32(),4),400,organizer.Token);
            await Send(HttpMethod.Post,"/api/events",Create(bookingId,max:26),400,organizer.Token);
            await Send(HttpMethod.Post,"/api/events",Create(bookingId,day:1),400,organizer.Token);
            await Send(HttpMethod.Post,"/api/events",Create(bookingId,tags:[tagId,tagId]),400,organizer.Token);
            await Send(HttpMethod.Post,"/api/events",Create(bookingId,tags:[int.MaxValue]),400,organizer.Token);
            var ev=await Send(HttpMethod.Post,"/api/events",Create(bookingId),201,organizer.Token);var eventId=ev.GetProperty("id").GetInt32();
            Assert.Equal(0,ev.GetProperty("status").GetInt32());
            Assert.Equal("Learning Group",ev.GetProperty("organizerCompany").GetString());
            await Send(HttpMethod.Post,"/api/events",Create(bookingId),409,organizer.Token);
            await Send(HttpMethod.Get,$"/api/events/{eventId}",null,404,attendee.Token);
            await Send(HttpMethod.Post,$"/api/events/{eventId}/publish",new{rowVersion=ev.GetProperty("rowVersion").GetString()},400,organizer.Token);
            object Update(string version,int max=20,int visibility=0,int[]?tags=null,int shift=0)=>new{title="Workshop Event",description="A practical programming workshop",startsAt=baseStart.AddHours(1).AddMinutes(shift),endsAt=baseStart.AddHours(3),maxAttendees=max,ticketPrice=0m,visibility,tagIds=tags??new[]{tagId},rowVersion=version};
            var oldVersion=ev.GetProperty("rowVersion").GetString()!;
            await Send(HttpMethod.Put,$"/api/events/{eventId}",Update(oldVersion),403,stranger.Token);
            ev=await Send(HttpMethod.Put,$"/api/events/{eventId}",Update(oldVersion),200,organizer.Token);
            await Send(HttpMethod.Put,$"/api/events/{eventId}",Update(oldVersion),409,organizer.Token);
            ev=await Send(HttpMethod.Post,$"/api/events/{eventId}/publish",new{rowVersion=ev.GetProperty("rowVersion").GetString()},200,organizer.Token);
            await Send(HttpMethod.Get,$"/api/events/{eventId}",null,200,attendee.Token);
            var listing=await Send(HttpMethod.Get,$"/api/events?city=Tbilisi&tagId={tagId}&freeOnly=true&search=Workshop&pageSize=1",null,200,attendee.Token);
            Assert.Equal(1,listing.GetProperty("totalCount").GetInt32());Assert.Equal(20,listing.GetProperty("items")[0].GetProperty("seatsLeft").GetInt32());
            Assert.False(listing.GetProperty("items")[0].TryGetProperty("accessCode",out _));
            await Send(HttpMethod.Get,"/api/events?page=0",null,400,attendee.Token);
            await Send(HttpMethod.Put,$"/api/events/{eventId}",Update(ev.GetProperty("rowVersion").GetString()!,tags:[]),400,organizer.Token);
            // Private event stays absent from every public listing; numeric ID never grants guest access.
            var privateBooking=await Booking(1);var privateBookingId=privateBooking.GetProperty("id").GetInt32();
            var privateEvent=await Send(HttpMethod.Post,"/api/events",Create(privateBookingId,1,1,[tagId]),201,organizer.Token);
            var privateId=privateEvent.GetProperty("id").GetInt32();var access=privateEvent.GetProperty("accessCode").GetString()!;Assert.Equal(64,access.Length);
            await Send(HttpMethod.Get,$"/api/private-events/{access}",null,404,attendee.Token);
            privateEvent=await Send(HttpMethod.Post,$"/api/events/{privateId}/publish",new{rowVersion=privateEvent.GetProperty("rowVersion").GetString()},200,organizer.Token);
            await Send(HttpMethod.Get,$"/api/private-events/{access}",null,401);
            await Send(HttpMethod.Get,$"/api/events/{privateId}",null,404,attendee.Token);
            var guestPrivate=await Send(HttpMethod.Get,$"/api/private-events/{access}",null,200,attendee.Token);
            Assert.Equal(JsonValueKind.Null,guestPrivate.GetProperty("accessCode").ValueKind);
            await Send(HttpMethod.Get,"/api/private-events/invalid",null,404,attendee.Token);
            Assert.Equal(1,(await Send(HttpMethod.Get,"/api/events",null,200,attendee.Token)).GetProperty("totalCount").GetInt32());
            Assert.Equal(2,(await Send(HttpMethod.Get,"/api/me/events",null,200,organizer.Token)).GetProperty("totalCount").GetInt32());
            // A draft can be deleted without deleting the booking, including explicit tag join rows.
            var draftBooking=await Booking(2);var draftBid=draftBooking.GetProperty("id").GetInt32();
            var draft=await Send(HttpMethod.Post,"/api/events",Create(draftBid,2,tags:[tagId]),201,organizer.Token);var draftId=draft.GetProperty("id").GetInt32();
            async Task Delete(int id,string? version,int status)
            {
                using var request=new HttpRequestMessage(HttpMethod.Delete,$"/api/events/{id}");request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",organizer.Token);
                if(version is not null)request.Headers.TryAddWithoutValidation("If-Match",$"\"{version}\"");
                using var response=await client.SendAsync(request);Assert.Equal(status,(int)response.StatusCode);
                if(status==204)Assert.Equal("",await response.Content.ReadAsStringAsync());
            }
            await Delete(draftId,null,400);await Delete(draftId,Convert.ToBase64String(new byte[8]),409);
            await Delete(eventId,ev.GetProperty("rowVersion").GetString(),400);
            await Delete(draftId,draft.GetProperty("rowVersion").GetString(),204);
            await Send(HttpMethod.Get,$"/api/events/{draftId}",null,404,organizer.Token);
            // Existing registrations protect time/visibility/capacity and count in SQL list projections.
            await using(var seed=new AppDbContext(options))
            {
                seed.RegistrationRecords.AddRange(new Registration{EventId=eventId,UserId=attendee.Id,RegisteredAt=DateTime.UtcNow},new Registration{EventId=eventId,UserId=stranger.Id,RegisteredAt=DateTime.UtcNow});
                await seed.SaveChangesAsync();
            }
            ev=await Send(HttpMethod.Get,$"/api/events/{eventId}",null,200,organizer.Token);
            Assert.Equal(18,ev.GetProperty("seatsLeft").GetInt32());
            Assert.Equal(18,(await Send(HttpMethod.Get,"/api/events",null,200,attendee.Token)).GetProperty("items")[0].GetProperty("seatsLeft").GetInt32());
            var version=ev.GetProperty("rowVersion").GetString()!;
            await Send(HttpMethod.Put,$"/api/events/{eventId}",Update(version,max:1),400,organizer.Token);
            await Send(HttpMethod.Put,$"/api/events/{eventId}",Update(version,shift:30),400,organizer.Token);
            await Send(HttpMethod.Put,$"/api/events/{eventId}",Update(version,visibility:1),400,organizer.Token);
            ev=await Send(HttpMethod.Post,$"/api/events/{eventId}/cancel",new{rowVersion=version},200,organizer.Token);
            Assert.Equal(2,ev.GetProperty("status").GetInt32());
            await Send(HttpMethod.Post,$"/api/events/{eventId}/publish",new{rowVersion=ev.GetProperty("rowVersion").GetString()},409,organizer.Token);
            Assert.Equal(1,(await Send(HttpMethod.Get,$"/api/bookings/{bookingId}",null,200,organizer.Token)).GetProperty("status").GetInt32());
            var currentBooking=await Send(HttpMethod.Get,$"/api/bookings/{privateBookingId}",null,200,owner.Token);
            await Send(HttpMethod.Post,$"/api/bookings/{privateBookingId}/cancel",new{reason="Space unavailable",rowVersion=currentBooking.GetProperty("rowVersion").GetString()},200,owner.Token);
            Assert.Equal(2,(await Send(HttpMethod.Get,$"/api/events/{privateId}",null,200,organizer.Token)).GetProperty("status").GetInt32());
            await Send(HttpMethod.Get,$"/api/private-events/{access}",null,404,attendee.Token);
            // Event completion is independent of a later booking end time.
            int finishedId,ongoingBookingId;
            await using(var seed=new AppDbContext(options))
            {
                var ongoing=new Booking{SpaceId=spaceId,UserId=organizer.Id,StartsAt=DateTime.UtcNow.AddHours(-3),EndsAt=DateTime.UtcNow.AddHours(2),AttendeeCount=20,Status=BookingStatus.Confirmed,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};
                var finished=new Event{Booking=ongoing,Title="Finished",Description="Finished event",StartsAt=DateTime.UtcNow.AddHours(-2),EndsAt=DateTime.UtcNow.AddHours(-1),MaxAttendees=20,Status=EventStatus.Published,CreatedAt=DateTime.UtcNow};
                seed.EventRecords.Add(finished);await seed.SaveChangesAsync();finishedId=finished.Id;ongoingBookingId=ongoing.Id;
            }
            var services=new ServiceCollection();services.AddLogging();services.AddApplication();services.AddInfrastructure(connection);
            await using(var provider=services.BuildServiceProvider())
            {await using var scope=provider.CreateAsyncScope();await scope.ServiceProvider.GetRequiredService<IEventService>().RefreshDueAsync(CancellationToken.None);}
            await using var verify=new AppDbContext(options);
            Assert.Equal(EventStatus.Completed,(await verify.EventRecords.SingleAsync(x=>x.Id==finishedId)).Status);
            Assert.Equal(BookingStatus.Confirmed,(await verify.BookingRecords.SingleAsync(x=>x.Id==ongoingBookingId)).Status);
            Assert.All(await verify.RegistrationRecords.Where(x=>x.EventId==eventId).ToListAsync(),x=>Assert.Equal(RegistrationStatus.Cancelled,x.Status));
            Assert.Equal(0,await verify.EventTagRecords.CountAsync(x=>x.EventId==draftId));
            Assert.Equal(BookingStatus.Confirmed,(await verify.BookingRecords.SingleAsync(x=>x.Id==draftBid)).Status);
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
