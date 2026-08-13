using Salesync.Application.Modules.Sales.Dtos.InvoiceReturn;

namespace Salesync.Application.Modules.Sales.Interfaces
{
    public interface IInvoiceReturnService
    {
        Task<IEnumerable<InvoiceReturnDto>> GetByInvoiceIdAsync(int invoiceId);
        Task<InvoiceReturnDto> GetByIdAsync(int id);
        Task<IEnumerable<InvoiceReturnDto>> GetBySalesRepIdAsync(int salesRepId);
        Task<IEnumerable<MobileReturnableInvoiceDto>> GetReturnableInvoicesAsync(int salesRepId, int customerId);
        Task<MobileReturnableInvoiceDetailsDto> GetReturnableInvoiceDetailsAsync(int salesRepId, int invoiceId);
        Task<InvoiceReturnDto> CreateAsync(CreateInvoiceReturnDto dto);
        Task<InvoiceReturnDto> ApproveAsync(int id);
        Task<InvoiceReturnDto> RejectAsync(int id);
        Task<InvoiceReturnDto> CancelAsync(int id);

    }
}
