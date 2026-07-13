using Salesync.Application.Modules.Treasury.Dtos;

namespace Salesync.Application.Modules.Treasury.Interfaces.Services
{
    public interface ITreasuryService
    {
        Task<IEnumerable<CashBoxDto>> GetCashBoxesAsync();

        Task<CashBoxDto> GetCashBoxByIdAsync(int id);

        Task<CashBoxDto> CreateCashBoxAsync(CreateCashBoxDto dto);

        Task<CashReceiptDto> ReceiveDayClosingCashAsync(int dayClosingId, ReceiveDayClosingCashDto dto);

        Task<IEnumerable<SalesRepCashLedgerDto>> GetSalesRepLedgerAsync(int salesRepId);

        Task<decimal> GetSalesRepCashBalanceAsync(int salesRepId);
    }
}