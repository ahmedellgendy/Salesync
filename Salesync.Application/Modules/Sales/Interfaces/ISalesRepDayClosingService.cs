using Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing;

namespace Salesync.Application.Modules.Sales.Interfaces
{
    public interface ISalesRepDayClosingService
    {
        Task<IEnumerable<SalesRepDayClosingDto>> GetAllAsync(SalesRepDayClosingFilterDto filter);

        Task<SalesRepDayClosingDto> GetByIdAsync(int id);

        Task<IEnumerable<SalesRepDayClosingDto>> GetBySalesRepAsync(int salesRepId);

        Task<SalesRepDayClosingDto> CreateAsync(CreateSalesRepDayClosingDto dto);

        Task<SalesRepDayClosingDto> ReceiveReturnedStockAsync(int id);

        Task CancelAsync(int id);
    }
}