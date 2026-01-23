using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Integration.Application.DTOs;

namespace Integration.Application.Interfaces;

public interface IInvoiceSyncService
{
    Task<InvoiceSyncResultDto> SyncInvoiceFromSapAsync(XontInvoiceSyncRequestDto request);
}
