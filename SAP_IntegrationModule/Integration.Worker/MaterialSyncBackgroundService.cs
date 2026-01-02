using Integration.Application.DTOs;
using Integration.Application.Interfaces;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Worker;

public class MaterialSyncBackgroundService : BackgroundService
{
    private readonly ILogger<MaterialSyncBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public MaterialSyncBackgroundService(ILogger<MaterialSyncBackgroundService> logger, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (LogContext.PushProperty("Service", nameof(MaterialSyncBackgroundService)))
        using (LogContext.PushProperty("Environment", _configuration["Environment"] ?? "Unknown"))
        {
            _logger.LogInformation("Material Sync Background Service started.");

            var enableSync = _configuration.GetValue<bool>("SyncSettings:EnableMaterialSync", true);
            if (!enableSync)
            {
                _logger.LogInformation("Material Sync is disabled via configuration. Stopping service.");
                return;
            }

            var dailySyncTime = _configuration.GetValue<string>("SyncSettings:DailySyncTime", "02:00:00");
            var syncIntervalMinutes = CalculateMinutesUntilNextRun(dailySyncTime);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting Material sync cycle .");
                    await PerformSyncWithRetryAsync(stoppingToken);
                    _logger.LogInformation("Material sync cycle completed .");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error occurred during Material sync cycle.");
                }
                try
                {
                    var delay = TimeSpan.FromMinutes(syncIntervalMinutes);
                    _logger.LogInformation("Material Sync Background Service waiting for {Delay} minutes before next cycle.", delay.TotalMinutes);
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Material Sync Background Service cancellation requested. Stopping.");
                    break;
                }
                
            }
        }
    }
    private async Task PerformSyncWithRetryAsync(CancellationToken stoppingToken)
    {
        const int maxRetries = 2;
        int retryCount = 0;

        while (true)
        {
            try
            {
                await PerformSyncAsync(stoppingToken);
                break;
            }
            catch (Exception ex)
            {
                retryCount++;

                if (retryCount > maxRetries)
                {
                    _logger.LogError(ex, "Material sync failed after {RetryCount} attempts. Giving up.", maxRetries + 1);
                    throw;
                }

                _logger.LogWarning(ex, "Material sync failed on attempt {Attempt}/{MaxRetries}. Retrying in 30 seconds...", retryCount, maxRetries + 1);

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
    private int CalculateMinutesUntilNextRun(string dailySyncTime)
    {
        var now = DateTime.Now;
        var syncTime = TimeSpan.Parse(dailySyncTime);
        var todaySync = now.Date.Add(syncTime);

        var nextRun = now < todaySync ? todaySync : todaySync.AddDays(1);
        var delay = nextRun - now;
        return (int)delay.TotalMinutes;
    }
    private async Task PerformSyncAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<IMaterialSyncService>();

        var request = new XontMaterialSyncRequestDto
        {
            Date = DateTime.Now.AddDays(-1).ToString("yyyyMMdd")
        };

        var result = await syncService.SyncMaterialsFromSapAsync(request);

        _logger.LogInformation(
            "Material sync completed: {Total} total, {New} new, {Updated} updated, Skipped: {Skipped}",
            result.TotalRecords, result.NewMaterials, result.UpdatedMaterials, result.SkippedMaterials);

    }
}