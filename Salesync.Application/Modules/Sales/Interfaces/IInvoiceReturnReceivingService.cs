using Salesync.Application.Modules.Sales.Dtos.InvoiceReturn;
namespace Salesync.Application.Modules.Sales.Interfaces;
public interface IInvoiceReturnReceivingService
{
    Task<ReturnReceivingDto> GetAsync(int returnId);
    Task<ReturnReceiptDto?> FindOperationAsync(int returnId, Guid operationId);
    Task<ReturnReceiptDto> ReceiveAsync(int returnId, ReceiveInvoiceReturnDto dto);
}
