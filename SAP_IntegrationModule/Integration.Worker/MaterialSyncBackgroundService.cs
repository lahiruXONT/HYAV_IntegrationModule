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
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

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


                    {
                }
                {
                }
            }
        }
        {
            var now = DateTime.Now;
            var syncTime = TimeSpan.Parse(dailySyncTime);
            var todaySync = now.Date.Add(syncTime);

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

        }
    }
}