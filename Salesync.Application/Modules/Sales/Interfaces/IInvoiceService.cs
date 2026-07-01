using Salesync.Application.Modules.Sales.Dtos.Invoice;

namespace Salesync.Application.Modules.Sales.Interfaces
{
    public interface IInvoiceService
    {
        Task<IEnumerable<InvoiceDto>> GetAllAsync();
        Task<InvoiceDto> GetByIdAsync(int id);
        Task<InvoiceDto> CreateAsync(CreateInvoiceDto dto);
        Task<InvoiceDto> UpdateAsync(int id, UpdateInvoiceDto dto);
        Task CancelAsync(int id);
    }
}
