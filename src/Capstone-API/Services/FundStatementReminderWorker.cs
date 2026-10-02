using BLL.Services.Implements.OperatingFund;

namespace Capstone_API.Services;

public sealed class FundStatementReminderWorker(IServiceScopeFactory scopeFactory, ILogger<FundStatementReminderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<FundStatementService>().RemindManagers();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Failed to send monthly fund statement reminders."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
