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

public sealed class RegistrationFlowTests
{
    [Fact]
    public async Task Registrations_limit_capacity_rotate_tickets_and_check_in_once()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "SpaceHub.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        var api = Path.Combine(root.FullName, "src", "SpaceHub.Api");
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var dll = Path.Combine(api, "bin", configuration, "net10.0", "SpaceHub.Api.dll");
        var databaseName = "SpaceHub_RegistrationTest_" + Guid.NewGuid().ToString("N");
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
            const string password="CoursePass!2026";
            async Task<(string Token,int Id)> Account(string email,string? role)
            {
                var result=await Send(HttpMethod.Post,"/api/auth/register",new{email,password,fullName="Ticket User"},201);
                var id=result.GetProperty("user").GetProperty("id").GetInt32();var token=result.GetProperty("accessToken").GetString()!;
                if(role is not null)token=(await Send(HttpMethod.Post,$"/api/me/roles/{role}",null,200,token)).GetProperty("accessToken").GetString()!;
                return(token,id);
            }
            var owner=await Account("owner@example.com","owner");
            var organizer=await Account("organizer@example.com","organizer");
            var otherOrganizer=await Account("other@example.com","organizer");
            var alice=await Account("alice@example.com",null);
            var bob=await Account("bob@example.com",null);
            await Send(HttpMethod.Put,"/api/me/organizer-profile",new{companyName="Ticket Events",phone="555123456",website="",description="Ticket tests"},200,organizer.Token);
            var space=await Send(HttpMethod.Post,"/api/spaces",new{name="Ticket Hall",description="A venue for tickets",address="Example Street 10",city="Tbilisi",capacity=40,hourlyRate=50},201,owner.Token);
            var spaceId=space.GetProperty("id").GetInt32();
            await Send(HttpMethod.Put,$"/api/spaces/{spaceId}/working-hours",new{days=Enumerable.Range(0,7).Select(d=>new{dayOfWeek=d,opensAt="08:00:00",closesAt="22:00:00"}),rowVersion=space.GetProperty("rowVersion").GetString()},200,owner.Token);
            var start=new DateTimeOffset(DateTime.UtcNow.Date.AddDays(7).AddHours(6),TimeSpan.Zero);
            async Task<(JsonElement Event,int BookingId)> MakeEvent(int offset,int max,int visibility,bool publish=true)
            {
                var startsAt=start.AddDays(offset);var endsAt=startsAt.AddHours(4);
                var quote=await Send(HttpMethod.Post,"/api/booking-quotes",new{spaceId,startsAt,endsAt,attendeeCount=25,packageId=(int?)null,additionalServices=Array.Empty<object>()},200,organizer.Token);
                var booking=await Send(HttpMethod.Post,"/api/bookings",new{spaceId,startsAt,endsAt,attendeeCount=25,packageId=(int?)null,additionalServices=Array.Empty<object>(),spaceRowVersion=quote.GetProperty("spaceRowVersion").GetString()},201,organizer.Token);
                var bookingId=booking.GetProperty("id").GetInt32();
                await Send(HttpMethod.Post,$"/api/bookings/{bookingId}/confirm",new{rowVersion=booking.GetProperty("rowVersion").GetString()},200,owner.Token);
                var ev=await Send(HttpMethod.Post,"/api/events",new{bookingId,title="Ticket Workshop",description="Workshop with registration",startsAt=startsAt.AddHours(1),endsAt=endsAt.AddHours(-1),maxAttendees=max,ticketPrice=0m,visibility,tagIds=new[]{1}},201,organizer.Token);
                if(publish)ev=await Send(HttpMethod.Post,$"/api/events/{ev.GetProperty("id").GetInt32()}/publish",new{rowVersion=ev.GetProperty("rowVersion").GetString()},200,organizer.Token);
                return(ev,bookingId);
            }
            var created=await MakeEvent(0,1,0);var ev=created.Event;var eventId=ev.GetProperty("id").GetInt32();
            var path=$"/api/events/{eventId}/registrations";
            await Send(HttpMethod.Post,path,new{},401);
            async Task<HttpResponseMessage> Register(string token)
            {
                using var request=new HttpRequestMessage(HttpMethod.Post,path){Content=JsonContent.Create(new{})};
                request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);return await client.SendAsync(request);
            }
            var responses=await Task.WhenAll(Register(alice.Token),Register(bob.Token));
            Assert.Equal(new[]{201,400},responses.Select(x=>(int)x.StatusCode).Order().ToArray());
            var winnerIndex=responses[0].IsSuccessStatusCode?0:1;
            var winner=winnerIndex==0?alice:bob;var loser=winnerIndex==0?bob:alice;
            var registration=await responses[winnerIndex].Content.ReadFromJsonAsync<JsonElement>();
            foreach(var response in responses)response.Dispose();
            var registrationId=registration.GetProperty("id").GetInt32();
            var firstCode=registration.GetProperty("ticket").GetProperty("code").GetString()!;
            Assert.Matches("^EVT-[A-Z0-9]{6}$",firstCode);
            Assert.Equal(winner.Id,registration.GetProperty("userId").GetInt32());
            Assert.False(registration.TryGetProperty("accessCode",out _));
            await Send(HttpMethod.Post,path,new{},409,winner.Token);
            Assert.Equal(0,(await Send(HttpMethod.Get,$"/api/events/{eventId}",null,200,alice.Token)).GetProperty("seatsLeft").GetInt32());
            Assert.Equal(1,(await Send(HttpMethod.Get,"/api/me/registrations?upcomingOnly=true",null,200,winner.Token)).GetProperty("totalCount").GetInt32());
            Assert.Equal(0,(await Send(HttpMethod.Get,"/api/me/registrations",null,200,loser.Token)).GetProperty("totalCount").GetInt32());
            await Send(HttpMethod.Get,"/api/me/registrations?pageSize=101",null,400,winner.Token);
            await Send(HttpMethod.Get,$"/api/events/{eventId}/attendees",null,403,winner.Token);
            await Send(HttpMethod.Get,$"/api/events/{eventId}/attendees",null,403,otherOrganizer.Token);
            var attendees=await Send(HttpMethod.Get,$"/api/events/{eventId}/attendees?status=0&search=example.com&pageSize=1",null,200,organizer.Token);
            Assert.Equal(1,attendees.GetProperty("totalCount").GetInt32());
            Assert.Equal(firstCode,attendees.GetProperty("items")[0].GetProperty("ticketCode").GetString());
            await Send(HttpMethod.Post,$"/api/tickets/{firstCode}/check-in",null,403,winner.Token);
            await Send(HttpMethod.Post,$"/api/tickets/{firstCode}/check-in",null,403,otherOrganizer.Token);
            await Send(HttpMethod.Post,$"/api/tickets/{firstCode}/check-in",null,400,organizer.Token);
            async Task Cancel(string token,int status)
            {
                using var request=new HttpRequestMessage(HttpMethod.Delete,path);request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);
                using var response=await client.SendAsync(request);Assert.Equal(status,(int)response.StatusCode);
                if(status==204)Assert.Equal("",await response.Content.ReadAsStringAsync());
            }
            await Cancel(loser.Token,404);await Cancel(winner.Token,204);await Cancel(winner.Token,409);
            await Send(HttpMethod.Post,$"/api/tickets/{firstCode}/check-in",null,400,organizer.Token);
            registration=await Send(HttpMethod.Post,path,new{},201,winner.Token);
            Assert.Equal(registrationId,registration.GetProperty("id").GetInt32());
            var secondCode=registration.GetProperty("ticket").GetProperty("code").GetString()!;
            Assert.NotEqual(firstCode,secondCode);
            await Send(HttpMethod.Post,$"/api/tickets/{firstCode}/check-in",null,404,organizer.Token);
            // Move the valid booking/event into the check-in window without waiting a week.
            await using(var seed=new AppDbContext(options))
            {
                var current=await seed.EventRecords.Include(x=>x.Booking).SingleAsync(x=>x.Id==eventId);
                current.Booking.StartsAt=DateTime.UtcNow.AddHours(-1);current.Booking.EndsAt=DateTime.UtcNow.AddHours(3);
                current.StartsAt=DateTime.UtcNow.AddHours(1);current.EndsAt=DateTime.UtcNow.AddHours(2);await seed.SaveChangesAsync();
            }
            async Task<HttpResponseMessage> CheckIn()
            {
                using var request=new HttpRequestMessage(HttpMethod.Post,$"/api/tickets/{secondCode}/check-in");request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",organizer.Token);
                return await client.SendAsync(request);
            }
            var checks=await Task.WhenAll(CheckIn(),CheckIn());
            Assert.Equal(new[]{200,409},checks.Select(x=>(int)x.StatusCode).Order().ToArray());foreach(var response in checks)response.Dispose();
            attendees=await Send(HttpMethod.Get,$"/api/events/{eventId}/attendees?status=0",null,200,organizer.Token);
            Assert.True(attendees.GetProperty("items")[0].GetProperty("isCheckedIn").GetBoolean());
            await Cancel(winner.Token,204);
            registration=await Send(HttpMethod.Post,path,new{},201,winner.Token);
            Assert.Equal(JsonValueKind.Null,registration.GetProperty("ticket").GetProperty("checkedInAt").ValueKind);
            var thirdCode=registration.GetProperty("ticket").GetProperty("code").GetString()!;
            Assert.NotEqual(secondCode,thirdCode);
            await Send(HttpMethod.Post,$"/api/tickets/{secondCode}/check-in",null,404,organizer.Token);
            ev=await Send(HttpMethod.Get,$"/api/events/{eventId}",null,200,organizer.Token);
            await Send(HttpMethod.Post,$"/api/events/{eventId}/cancel",new{rowVersion=ev.GetProperty("rowVersion").GetString()},200,organizer.Token);
            await Send(HttpMethod.Post,$"/api/tickets/{thirdCode}/check-in",null,400,organizer.Token);
            await Send(HttpMethod.Post,path,new{},400,winner.Token);
            Assert.Equal(0,(await Send(HttpMethod.Get,"/api/me/registrations?upcomingOnly=true",null,200,winner.Token)).GetProperty("totalCount").GetInt32());
            // Private registration requires the actual access code, even if numeric event ID is known.
            var privateCreated=await MakeEvent(1,2,1);var privateEvent=privateCreated.Event;var privateId=privateEvent.GetProperty("id").GetInt32();
            var privateCode=privateEvent.GetProperty("accessCode").GetString();
            await Send(HttpMethod.Post,$"/api/events/{privateId}/registrations",new{},404,alice.Token);
            await Send(HttpMethod.Post,$"/api/events/{privateId}/registrations",new{accessCode=new string('A',64)},404,alice.Token);
            var privateRegistration=await Send(HttpMethod.Post,$"/api/events/{privateId}/registrations",new{accessCode=privateCode,userId=bob.Id},201,alice.Token);
            Assert.Equal(alice.Id,privateRegistration.GetProperty("userId").GetInt32());
            var privateTicket=privateRegistration.GetProperty("ticket").GetProperty("code").GetString()!;
            var privateBooking=await Send(HttpMethod.Get,$"/api/bookings/{privateCreated.BookingId}",null,200,owner.Token);
            await Send(HttpMethod.Post,$"/api/bookings/{privateCreated.BookingId}/cancel",new{reason="Venue issue",rowVersion=privateBooking.GetProperty("rowVersion").GetString()},200,owner.Token);
            await Send(HttpMethod.Post,$"/api/tickets/{privateTicket}/check-in",null,400,organizer.Token);
            var draft=await MakeEvent(2,2,0,false);
            await Send(HttpMethod.Post,$"/api/events/{draft.Event.GetProperty("id").GetInt32()}/registrations",new{},400,alice.Token);
            // Registration closes at event start; cancellation and check-in end limits use real time.
            var ended=await MakeEvent(3,2,0);var endedId=ended.Event.GetProperty("id").GetInt32();
            var endedRegistration=await Send(HttpMethod.Post,$"/api/events/{endedId}/registrations",new{},201,alice.Token);
            var endedTicket=endedRegistration.GetProperty("ticket").GetProperty("code").GetString()!;
            await using(var seed=new AppDbContext(options))
            {var e=await seed.EventRecords.SingleAsync(x=>x.Id==endedId);e.StartsAt=DateTime.UtcNow.AddHours(-2);e.EndsAt=DateTime.UtcNow.AddMinutes(-1);await seed.SaveChangesAsync();}
            await Send(HttpMethod.Post,$"/api/events/{endedId}/registrations",new{},400,bob.Token);
            await Send(HttpMethod.Post,$"/api/tickets/{endedTicket}/check-in",null,400,organizer.Token);
            using(var request=new HttpRequestMessage(HttpMethod.Delete,$"/api/events/{endedId}/registrations"))
            {request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",alice.Token);using var response=await client.SendAsync(request);Assert.Equal(400,(int)response.StatusCode);}
            await using var verify=new AppDbContext(options);
            Assert.Equal(1,await verify.RegistrationRecords.CountAsync(x=>x.EventId==eventId&&x.UserId==winner.Id));
            Assert.Equal(1,await verify.TicketRecords.CountAsync(x=>x.RegistrationId==registrationId));
            Assert.Equal(RegistrationStatus.Cancelled,(await verify.RegistrationRecords.SingleAsync(x=>x.Id==registrationId)).Status);
            Assert.Equal(thirdCode,(await verify.TicketRecords.SingleAsync(x=>x.RegistrationId==registrationId)).Code);
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
