using ClosingEntity = Salesync.Domain.Modules.Sales.Entities.SalesRepDayClosing;

namespace Salesync.Application.Modules.Sales.Interfaces
{
    public interface ISalesRepDayClosingSettlementService
    {
        Task ApplyApprovalSettlementAsync(ClosingEntity closing);
    }
}