using Microsoft.EntityFrameworkCore;
using SpaceHub.Domain.Entities;
using SpaceHub.Domain.Entities.Enums;
using SpaceHub.Infrastructure.Auth;
using SpaceHub.Infrastructure.Data;
using Xunit;

namespace SpaceHub.IntegrationTests;

public sealed class DemoSeedTests
{
    [Fact]
    public async Task Cli_imports_once_and_refuses_production()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "SpaceHub.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        var api = Path.Combine(root.FullName, "src", "SpaceHub.Api");
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var name = "SpaceHub_DemoCliTest_" + Guid.NewGuid().ToString("N");
        var connection = $"Server=(localdb)\\MSSQLLocalDB;Database={name};AttachDbFilename={Path.Combine(Path.GetTempPath(), name + ".mdf")};Trusted_Connection=True;TrustServerCertificate=True";
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options);
        async Task<(int Code, string Output)> Run(string environment)
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo("dotnet")
            { WorkingDirectory = api, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            process.StartInfo.ArgumentList.Add(Path.Combine(api, "bin", configuration, "net10.0", "SpaceHub.Api.dll"));
            process.StartInfo.ArgumentList.Add("--seed-demo");
            process.StartInfo.Environment["ASPNETCORE_ENVIRONMENT"] = environment;
            process.StartInfo.Environment["DOTNET_ENVIRONMENT"] = environment;
            process.StartInfo.Environment["ConnectionStrings__DefaultConnection"] = connection;
            process.StartInfo.Environment["Logging__LogLevel__Default"] = "Warning";
            process.Start();
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            try { await process.WaitForExitAsync(timeout.Token); }
            finally { if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync(); } }
            return (process.ExitCode, await stdout + await stderr);
        }
        try
        {
            var production = await Run("Production");
            Assert.NotEqual(0, production.Code);
            Assert.Contains("only be imported in Development", production.Output);
            await using (var master = new Microsoft.Data.SqlClient.SqlConnection("Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True"))
            {
                await master.OpenAsync();
                await using var query = master.CreateCommand();
                query.CommandText = "SELECT DB_ID(@name)";
                query.Parameters.AddWithValue("@name", name);
                Assert.Equal(DBNull.Value, await query.ExecuteScalarAsync());
            }
            var first = await Run("Development");
            Assert.True(first.Code == 0, first.Output);
            Assert.Contains("Demo data imported", first.Output);
            var second = await Run("Development");
            Assert.True(second.Code == 0, second.Output);
            Assert.Contains("Demo import skipped", second.Output);
            Assert.Equal(3, await db.UserRecords.CountAsync());
            Assert.Equal(3, await db.TicketRecords.CountAsync());
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Import_is_atomic_repeatable_and_preserves_existing_accounts(bool reservedCollision)
    {
        var name = "SpaceHub_DemoTest_" + Guid.NewGuid().ToString("N");
        var file = Path.Combine(Path.GetTempPath(), name + ".mdf");
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(
            $"Server=(localdb)\\MSSQLLocalDB;Database={name};AttachDbFilename={file};Trusted_Connection=True;TrustServerCertificate=True").Options;
        await using var db = new AppDbContext(options);
        try
        {
            await db.Database.MigrateAsync();
            var original = new User { Email = reservedCollision ? DemoDataSeeder.Emails[0] : "existing@example.com",
                FullName = "Existing user", PasswordHash = "unchanged", CreatedAt = DateTime.UtcNow };
            db.Add(original);
            await db.SaveChangesAsync();
            var passwords = new PasswordService();
            var seeder = new DemoDataSeeder(db, passwords);
            Assert.Equal(!reservedCollision, await seeder.SeedAsync());
            db.ChangeTracker.Clear();
            if (reservedCollision)
            {
                Assert.Equal(1, await db.UserRecords.CountAsync());
                Assert.Empty(await db.SpaceRecords.ToListAsync());
                Assert.Empty(await db.UserRoleRecords.ToListAsync());
                Assert.Equal("unchanged", (await db.UserRecords.SingleAsync()).PasswordHash);
                return;
            }
            Assert.Equal(4, await db.UserRecords.CountAsync());
            Assert.Equal(2, await db.SpaceRecords.CountAsync());
            Assert.Equal(3, await db.SpaceServiceRecords.CountAsync());
            Assert.Equal(1, await db.PackageRecords.CountAsync());
            Assert.Equal(4, await db.BookingRecords.CountAsync());
            Assert.Equal(3, await db.EventRecords.CountAsync());
            Assert.Equal(3, await db.RegistrationRecords.CountAsync());
            Assert.Equal(3, await db.TicketRecords.CountAsync());
            foreach (var user in await db.UserRecords.Where(x => DemoDataSeeder.Emails.Contains(x.Email)).ToListAsync())
                Assert.True(passwords.Verify(user, DemoDataSeeder.Password, out _));
            var booking = await db.BookingRecords.Include(x => x.Services).SingleAsync(x => x.PackageId != null);
            Assert.Equal(550m, booking.TotalAmount);
            Assert.Equal(booking.TotalAmount, booking.SpaceSubtotal + booking.PackagePriceSnapshot + booking.Services.Sum(x => x.Subtotal));
            Assert.Equal(25, booking.Services.Where(x => x.PricingTypeSnapshot == ServicePricingType.PerPerson).Sum(x => x.Quantity));
            var meetings = await db.EventRecords.Include(x => x.Booking).ToListAsync();
            Assert.All(meetings, meeting =>
            {
                Assert.True(meeting.StartsAt > DateTime.UtcNow);
                Assert.True(meeting.StartsAt >= meeting.Booking.StartsAt && meeting.EndsAt <= meeting.Booking.EndsAt);
                Assert.Equal(BookingStatus.Confirmed, meeting.Booking.Status);
                Assert.True(meeting.MaxAttendees <= meeting.Booking.AttendeeCount);
            });
            Assert.Equal(64, meetings.Single(x => x.Visibility == EventVisibility.Private).AccessCode!.Length);
            var tickets = await db.TicketRecords.ToListAsync();
            Assert.Equal(3, tickets.Select(x => x.Code).Distinct().Count());
            Assert.All(tickets, ticket => Assert.Matches("^EVT-[A-Z0-9]{6}$", ticket.Code));
            var space = await db.SpaceRecords.FirstAsync();
            space.Name = "User edited this space";
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            Assert.False(await seeder.SeedAsync());
            Assert.Equal("User edited this space", (await db.SpaceRecords.FindAsync(space.Id))!.Name);
            Assert.Equal(4, await db.UserRecords.CountAsync());
            Assert.Equal(4, await db.BookingRecords.CountAsync());
            Assert.Equal(3, await db.TicketRecords.CountAsync());
            Assert.Equal("unchanged", (await db.UserRecords.FindAsync(original.Id))!.PasswordHash);
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
