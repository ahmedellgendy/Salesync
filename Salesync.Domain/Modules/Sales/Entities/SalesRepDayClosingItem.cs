using Salesync.Domain.Common;
using Salesync.Domain.Modules.MasterData.Entities;

namespace Salesync.Domain.Modules.Sales.Entities
{
    public class SalesRepDayClosingItem : BaseEntity
    {
        public int SalesRepDayClosingId { get; set; }
        public int ProductId { get; set; }
        public int ExpectedRemainingQuantity { get; set; }
        public int ActualReturnedQuantity { get; set; }
        public int VarianceQuantity { get; set; }
        public string? Notes { get; set; }

        public SalesRepDayClosing SalesRepDayClosing { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}