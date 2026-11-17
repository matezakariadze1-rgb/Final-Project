using SpaceHub.Application.Bookings;
namespace SpaceHub.Api.Background;

public sealed class BookingMaintenanceWorker : BackgroundService
{
    private readonly IServiceScopeFactory scopes;
    private readonly ILogger<BookingMaintenanceWorker> logger;


    public BookingMaintenanceWorker(IServiceScopeFactory scopes, ILogger<BookingMaintenanceWorker> logger)
    {
        this.scopes = scopes;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IBookingService>().RefreshDueAsync(stoppingToken);
                using var eventScope = scopes.CreateScope();
                await eventScope.ServiceProvider.GetRequiredService<SpaceHub.Application.Events.IEventService>().RefreshDueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Could not refresh expired bookings; will retry next cycle."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
