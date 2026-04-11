using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.HostedServices;

public class ReconciliationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReconciliationBackgroundService> _logger;

    public ReconciliationBackgroundService(IServiceProvider serviceProvider, ILogger<ReconciliationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReconciliationBackgroundService started. Waiting 5 minutes for initial delay...");
        
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Starting global background reconciliation scan.");
            
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
                var reconciliationService = scope.ServiceProvider.GetRequiredService<IReconciliationService>();

                var companies = await context.Companies.Select(c => c.Id).ToListAsync(stoppingToken);

                foreach (var companyId in companies)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    try
                    {
                        var watch = System.Diagnostics.Stopwatch.StartNew();
                        await reconciliationService.RunCompanyReconciliationAsync(companyId);
                        watch.Stop();
                        
                        _logger.LogInformation($"Reconciliation cycle completed for Company {companyId} in {watch.ElapsedMilliseconds} ms.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Critical failure during reconciliation scan for CompanyId {companyId}.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Fatal error obtaining scope or database context in Background Reconciliation loop.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
        
        _logger.LogInformation("ReconciliationBackgroundService is stopping.");
    }
}
