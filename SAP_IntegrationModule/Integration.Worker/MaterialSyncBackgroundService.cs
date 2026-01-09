using Integration.Application.DTOs;
using Integration.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Integration.Worker;

public class MaterialSyncBackgroundService : ResilientBackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public MaterialSyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<MaterialSyncBackgroundService> logger,
        IOptionsMonitor<BackgroundServiceOptions> optionsMonitor
    )
        : base(logger, optionsMonitor, nameof(MaterialSyncBackgroundService))
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteCycleAsync(CancellationToken stoppingToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var syncService = scope.ServiceProvider.GetRequiredService<IMaterialSyncService>();

        var request = new XontMaterialSyncRequestDto
        {
            Date = DateTime.UtcNow.AddDays(-1).ToString("yyyyMMdd"),
        };

        var result = await syncService.SyncMaterialsFromSapAsync(request);

        if (result.Success)
        {
            _logger.LogInformation("Material sync completed {@Result}", result);
        }
        else
        {
            _logger.LogWarning("Material sync completed with issues {@Result}", result);

            if (result.TotalRecords > 0 && result.NewMaterials + result.UpdatedMaterials == 0)
            {
                throw new InvalidOperationException(
                    $"Material sync processed zero records. {result.Message}"
                );
            }
        }
    }
}
