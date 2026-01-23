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
        try
        {
            var request = new XontMaterialSyncRequestDto
            {
                Date = DateTime.Now.AddDays(-1).ToString("yyyyMMdd"),
            };

            var result = await syncService.SyncMaterialsFromSapAsync(request);

            if (result.Success)
            {
                _logger.LogInformation("Material sync completed {@Result}", result);
            }
            else
            {
                _logger.LogWarning("Material sync completed with issues {@Result}", result);
            }
        }
        catch (SapApiExceptionDto ex)
        {
            _logger.LogError("Material sync failed with SAP Issue");
            throw;
        }
        catch (MaterialSyncException ex)
        {
            _logger.LogError("Material sync failed with Issues");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Material sync failed with issues");
            throw;
        }
    }
}
