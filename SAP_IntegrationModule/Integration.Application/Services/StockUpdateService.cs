using System.Transactions;
using Integration.Application.DTOs;
using Microsoft.Extensions.Logging;
using XONT.Common.Message;
using XONT.VENTURA.STTRN01_V5.Application;
using XONT.VENTURA.STTRN01_V5.Domain;

namespace Integration.Application.Services;

//Stock Added to Main Warehouse - We dont know, added via SAP
//Stock out done from Main Warehouse, Doc No generated - We dont know, done via SAP
//When doing stock in other location, need to Enter Doc No - Done from XONT backend
//  - Step 1 -> Call Material Stock API, and sync Main Warehouse Stock for XONT Database (record as Manual Stock In)
//  - Step 2 -> Record the Stock out for main warehouse (Stock Allocate)
//  * Stock in for other location is handled by XONT backend as usual (Stock Update for Main warehouse and deallocation, stock Update for location warehouse).
//  - Step 3 -> After Stock In done, send Stock In details to SAP

public class StockUpdateService
{
    private readonly StockManager _stManager;
    private readonly ILogger<StockUpdateService> _logger;

    public StockUpdateService(StockManager stManager, ILogger<StockUpdateService> logger)
    {
        _stManager = stManager;
        _logger = logger;
    }

    public bool UpdateStock(
        StockOutSapResponseDto sapStockDetails,
        string workstationID,
        out List<string> errorLog,
        out MessageSet msg
    )
    {
        msg = null;
        errorLog = new List<string>();
        bool deallocate = false;

        try
        {
            string businessUnit = sapStockDetails.Division; //////CHECK
            string movementType = "0";

            Stock stock = new();
            stock = CreateStockObjects(sapStockDetails, deallocate);

            RdControl controlInfo = new RdControl();
            controlInfo = _stManager.GetRdControl(businessUnit, ref msg);

            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = TransactionManager.DefaultTimeout,
            };

            using var scope = new TransactionScope(
                TransactionScopeOption.Required,
                transactionOptions,
                TransactionScopeAsyncFlowOption.Enabled
            );

            // Stock Deallocation
            if (controlInfo.TransferRequestAllocateQty == "1" && movementType == "0")
            {
                deallocate = true;
                _logger.LogInformation(
                    "Starting stock deallocation for DocNo: {TransferNo}",
                    sapStockDetails.MaterialDocumentNumber
                );

                var allocList = new List<Allocation>();
                var stockDeAlloc = CreateStockObjects(sapStockDetails, deallocate);

                _stManager.StockAllocate(
                    businessUnit,
                    "SapIntegration",
                    stockDeAlloc,
                    continueWithErrors: false,
                    ref allocList,
                    ref msg
                );

                if (msg != null)
                {
                    _logger.LogWarning(
                        "Stock deallocation failed for DocNo: {TransferNo}",
                        sapStockDetails.MaterialDocumentNumber
                    );

                    return false;
                }
            }

            // 🔹 Stock Allocation
            if (stock.StockProductList.Any())
            {
                foreach (var product in stock.StockProductList)
                {
                    foreach (var batch in product.ProductBatchSerialList)
                    {
                        if (batch.BatchSerialPrice == 0)
                        {
                            batch.BatchSerialPrice = batch.BatchTransactionCost;
                        }
                    }
                }

                _logger.LogInformation(
                    "Starting stock update for for DocNo: {TransferNo}",
                    sapStockDetails.MaterialDocumentNumber
                );

                _stManager.StockUpdate(businessUnit, "SapIntegration", stock, ref msg);

                if (msg != null)
                {
                    _logger.LogWarning(
                        "Stock update failed for DocNo: {TransferNo}",
                        sapStockDetails.MaterialDocumentNumber
                    );

                    return false;
                }
            }

            scope.Complete();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled error in UpdateStock for DocNo: {TransferNo}",
                sapStockDetails.MaterialDocumentNumber
            );

            msg = MessageCreate.CreateErrorMessage(
                0,
                ex,
                nameof(UpdateStock),
                "Integration.Application"
            );

            return false;
        }
    }

    private Stock CreateStockObjects(StockOutSapResponseDto sapStockDetails, bool deallocate)
    {
        decimal movementQty = deallocate
            ? (-1) * sapStockDetails.Quantity
            : sapStockDetails.Quantity;
        var stock = new Stock
        {
            BusinessUnit = sapStockDetails.Division,
            MovementReason = "",
            Territory = "",
            TransactionDate = sapStockDetails.PostingDate,
            TransactionEnteredDate = DateTime.Now.Date,
            Warehouse = sapStockDetails.Plant,
            TRNTypeHeaderNumber = 0,
            Location = sapStockDetails.StorageLocation,
            MovementReference = "",
            TransactionReference = sapStockDetails.MaterialDocumentNumber,
            TRNTypeRef = "",
            TransactionCode = 99,
            MovementType = "",
            TRNType = "",
        };

        var serial = new ProductBatchSerial
        {
            ProductCode = sapStockDetails.Material,
            Quantity = movementQty,
            LotNumber = "",
            BatchSerial = sapStockDetails.ReceivingBatch,
            BatchSerialPrice = 0,
            BatchTransactionCost = 0,
            BatchDate = sapStockDetails.PostingDate,
            ExpiryDate = sapStockDetails.BatchExpiryDate,
            TerritoryCode = "",
            WarehouseCode = sapStockDetails.Plant,
            LocationCode = sapStockDetails.StorageLocation,
            MovementType = "",
        };

        var stockProduct = new StockProduct
        {
            BatchSerialControlItem = "",
            MovementQuantity = movementQty,
            MovementReason = "",
            ProductCode = sapStockDetails.Material,
            Territory = "",
            TRNTypeDetailNumber = 0,
            UOM = "",
            UOMType = "",
            Warehouse = sapStockDetails.Plant,
            ProductBatchSerialList = new List<ProductBatchSerial> { serial },
            StandardCost = 0,
            Price = 0,
        };

        stock.StockProductList = new List<StockProduct> { stockProduct };

        return stock;
    }
}
