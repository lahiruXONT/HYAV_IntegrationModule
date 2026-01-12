using Integration.Application.Helpers;
using Integration.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Integration.Application.Services;

public sealed class SalesSyncService
{
    private readonly ISalesRepository _salesRepository;
    private readonly ISapClient _sapClient;

    //private readonly CustomerMappingHelper _mappingHelper;
    private readonly ILogger<CustomerSyncService> _logger;

    public SalesSyncService(
        ISalesRepository salesRepository,
        ISapClient sapClient,
        CustomerMappingHelper mappingHelper,
        ILogger<CustomerSyncService> logger
    )
    {
        _salesRepository =
            salesRepository ?? throw new ArgumentNullException(nameof(salesRepository));
        _sapClient = sapClient ?? throw new ArgumentNullException(nameof(sapClient));
        //_mappingHelper = mappingHelper ?? throw new ArgumentNullException(nameof(mappingHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    //3) Send Sales Orders:
    //-Take unprocessed orders
    //-Format according to SAP DTO
    //-Send to SAP
    //-Check response and update status

    //4) Get Sales Invoices:
    //-Request data
    //-Take Invoice Details
    //-Map with order details(need to clarify)
    //-Update Invoice Tables and Order Status
    //-Update Inquiry Table(New)

    //Send Sales Orders
    private void GetUprocessedOrders() { }

    private void MapOrderToDTO() { }

    private void TransferOrderToSAP() { }

    private void UpdateOrderStatus() { }

    //Get Sales Invoices
    private void GetSalesInvoicesFromSAP() { }

    private void MapDTOInvoiceToDomainObject() { }

    private void SaveInvoiceDetails() { }

    private void UpdateInvoiceStatus() { }
}
