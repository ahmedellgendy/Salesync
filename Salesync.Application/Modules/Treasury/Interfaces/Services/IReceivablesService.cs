using Salesync.Application.Modules.Treasury.Dtos.Receivables;

namespace Salesync.Application.Modules.Treasury.Interfaces.Services
{
    public interface IReceivablesService
    {
        Task<ReceivablesSummaryDto> GetSummaryAsync();

        Task<IEnumerable<CustomerReceivableDto>>
            GetCustomerReceivablesAsync();

        Task<IEnumerable<CustomerReceivableInvoiceDto>>
            GetCustomerReceivableDetailsAsync(
                int customerId);

        Task<IEnumerable<SalesRepReceivableDto>>
            GetSalesRepReceivablesAsync();

        Task<SalesRepDebtPaymentResultDto> PaySalesRepDebtAsync(
        int salesRepId,
        PaySalesRepDebtDto dto);
    }
}