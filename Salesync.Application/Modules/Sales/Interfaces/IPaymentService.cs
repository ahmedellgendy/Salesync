using Salesync.Application.Modules.Sales.Dtos.Payment;

namespace Salesync.Application.Modules.Sales.Interfaces
{
    public interface IPaymentService
    {
        Task<IEnumerable<PaymentDto>> GetAllAsync();

        Task<PaymentDto> GetByIdAsync(int id);

        Task<IEnumerable<PaymentDto>> GetByInvoiceIdAsync(int invoiceId);

        Task<PaymentDto> CreateAsync(CreatePaymentDto dto);
    }
}