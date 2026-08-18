using BLL.Common;
using DAL;

namespace Capstone_API.Services;

public sealed class ShiftLifecycleWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ShiftLifecycleWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await ShiftLifecycle.CompleteEndedShiftsAsync(context);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to synchronize ended shifts and operational teams.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
