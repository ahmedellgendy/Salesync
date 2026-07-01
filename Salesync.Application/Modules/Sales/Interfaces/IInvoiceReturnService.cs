using Salesync.Application.Modules.Sales.Dtos.InvoiceReturn;

namespace Salesync.Application.Modules.Sales.Interfaces
{
    public interface IInvoiceReturnService
    {
        Task<IEnumerable<InvoiceReturnDto>> GetByInvoiceIdAsync(int invoiceId);
        Task<InvoiceReturnDto> CreateAsync(CreateInvoiceReturnDto dto);
        Task<InvoiceReturnDto> ApproveAsync(int id);
        Task<InvoiceReturnDto> RejectAsync(int id);
    }
}
