using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Integration.Application.DTOs;

namespace Integration.Application.Interfaces;

public interface IReceiptSyncService
{
    Task<ReceiptSyncResultDto> SyncReceiptToSapAsync(XontReceiptSyncRequestDto request);
}
