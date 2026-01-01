using Integration.Application.DTOs;
using Integration.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Worker
{
    public class MaterialSyncBackgroundService : BackgroundService
    {
        private readonly ILogger<MaterialSyncBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public MaterialSyncBackgroundService(ILogger<MaterialSyncBackgroundService> logger, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Material Sync Background Service  started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformSyncAsync(stoppingToken);


                    var dailySyncTime = _configuration["SyncSettings:DailySyncTime"] ?? "02:00:00";
                    var nextRun = GetNextRunTime(dailySyncTime);
                    var delay = nextRun - DateTime.Now;

                    if (delay > TimeSpan.Zero)
                    {
                        _logger.LogInformation("Next material sync scheduled for {NextRun}", nextRun);
                        await Task.Delay(delay, stoppingToken);
                    }
                }
                catch (Exception ex) when (ex is not TaskCanceledException)
                {
                    _logger.LogError(ex, "Error in material sync background service");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }

        private DateTime GetNextRunTime(string dailySyncTime)
        {
            var now = DateTime.Now;
            var syncTime = TimeSpan.Parse(dailySyncTime);
            var todaySync = now.Date.Add(syncTime);

            return now < todaySync ? todaySync : todaySync.AddDays(1);
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
                "Material sync completed: {Total} total, {New} new, {Updated} updated",
                result.TotalRecords, result.NewMaterials, result.UpdatedMaterials);

        }
    }
}