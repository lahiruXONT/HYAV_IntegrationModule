using Integration.Application.DTOs;
using Integration.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Integration.Worker;

public sealed class SalesSyncBackgroundService : ResilientBackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public SalesSyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SalesSyncBackgroundService> logger,
        IOptionsMonitor<BackgroundServiceOptions> optionsMonitor
    )
        : base(logger, optionsMonitor, nameof(SalesSyncBackgroundService))
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteCycleAsync(CancellationToken stoppingToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var syncService = scope.ServiceProvider.GetRequiredService<ISalesSyncService>();

        var request = new XontSalesSyncRequestDto { Date = DateTime.UtcNow.AddDays(-1) };

        var result = await syncService.SyncSalesOrderToSapAsync(request);

        if (result.Success)
        {
            _logger.LogInformation("Sales sync completed {@Result}", result);
        }
        else
        {
            _logger.LogWarning("Sales sync completed with issues {@Result}", result);

            if (result.TotalRecords > 0 && result.NewOrders + result.UpdatedOrders == 0)
            {
                throw new InvalidOperationException(
                    $"Sales sync processed zero records. {result.Message}"
                );
            }
        }
    }
}
