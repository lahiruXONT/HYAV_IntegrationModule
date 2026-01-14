using Integration.Application.DTOs;
using Integration.Application.Interfaces;
using Integration.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Integration.Application.Helpers;

public sealed class StockMappingHelper
{
    private readonly ILogger<StockMappingHelper> _logger;
    private readonly IStockRepository _stockRepository;

    public StockMappingHelper(ILogger<StockMappingHelper> logger, IStockRepository stockRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stockRepository =
            stockRepository ?? throw new ArgumentNullException(nameof(stockRepository));
    }

    public async Task<StockTransaction> MapSapToXontStockTransactionAsync(
        StockOutSapResponseDto stock
    )
    {
        try
        {
            var stockTransaction = new StockTransaction
            {
                // Business context
                BusinessUnit = stock.Division ?? string.Empty,
                TerritoryCode = "DEFAULT-TERR", // Hardcoded or mapped from Division/Plant if you have rules
                TransactionCode = 7, // Example: Outbound transaction code

                // Dates
                TransactionDate = stock.PostingDate,
                TransactionEnteredDate = stock.EnteredOnAt,

                // Warehouse / Location
                WarehouseCode = stock.Plant ?? string.Empty,
                LocationCode = stock.StorageLocation ?? string.Empty,

                // Product
                ProductCode = stock.Material ?? string.Empty,

                // Movement quantities
                U1MovementQuantity = stock.Quantity,
                Uom1PostTransactionStock = 0, // You may calculate based on inventory
                Uom1 = "EA", // Example: Each
                U2MovementQuantity = 0, // Optional secondary UOM
                Uom2PostTransactionStock = 0,
                Uom2 = string.Empty,

                // Pricing and costing (mocked for now)
                ConversionRate = 1,
                Price = 0,
                Cost = 0,
                PostTransactionAverageCost = 0,
                PostTransactionAverageCostLocation = 0,
                BatchCost = 0,
                StandardCost = 0,

                // References
                MovementReference = stock.ReceivingBatch ?? string.Empty,
                TransactionReference = stock.MaterialDocumentNumber ?? string.Empty,
                MovementReason = "Stock Out",
                TrnType = "OUT",

                // Defaults
                Status = "1",
                PdaTransaction = "0",
            };

            await ValidateXontStockTransactionAsync(stockTransaction);
            return stockTransaction;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to map SAP stock {Code}. Division: {Division}, Plant: {Plant}, StorageLocation: {StorageLocation}",
                stock?.Material,
                stock?.Division,
                stock?.Plant,
                stock?.StorageLocation
            );
            throw;
        }
    }

    private async Task ValidateXontStockTransactionAsync(StockTransaction stockTransaction)
    {
        //if (sapOrder == null)
        //    throw new ValidationExceptionDto("SAP order data cannot be null");

        //var errors = new List<string>();

        //if (string.IsNullOrWhiteSpace(sapOrder.YourReference))
        //    errors.Add("OrderNo is required");

        //if (sapOrder.Items?.Count <= 0)
        //    errors.Add($"Items missing in order: {sapOrder.YourReference}");

        //if (string.IsNullOrWhiteSpace(sapOrder.SoldToParty))
        //    errors.Add("Customer is required");

        //if (string.IsNullOrWhiteSpace(sapOrder.SalesOrg))
        //    errors.Add("Sales organization is required");

        //if (string.IsNullOrWhiteSpace(sapOrder.Division))
        //    errors.Add("Division is required");

        //if (
        //    !string.IsNullOrWhiteSpace(sapOrder.Division)
        //    && !string.IsNullOrWhiteSpace(sapOrder.SalesOrg)
        //)
        //{
        //    var exists = await _businessUnitResolver.SalesOrgDivisionExistsAsync(
        //        sapOrder.SalesOrg,
        //        sapOrder.Division
        //    );

        //    if (!exists)
        //    {
        //        errors.Add(
        //            $"Business unit not found for SalesOrg: '{sapOrder.SalesOrg}' Division: '{sapOrder.Division}'"
        //        );
        //    }
        //}

        //if (string.IsNullOrWhiteSpace(sapOrder.SalesOffice))
        //{
        //    errors.Add("SalesOffice is required");
        //}

        //if (errors.Any())
        //{
        //    var errorMessage = string.Join("; ", errors);
        //    throw new ValidationExceptionDto(errorMessage);
        //}
    }
}
