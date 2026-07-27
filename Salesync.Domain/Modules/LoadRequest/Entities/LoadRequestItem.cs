using Salesync.Domain.Common;
using Salesync.Domain.Modules.MasterData.Entities;

namespace Salesync.Domain.Modules.LoadRequest.Entities
{
    public class LoadRequestItem : BaseEntity
    {
        public int LoadRequestId { get; set; }
        public int ProductId { get; set; }
        public required string ProductName { get; set; }
        public required string ItemCode { get; set; }
        public int RequestedQuantity { get; set; }
        public int ApprovedQuantity { get; set; }
        public int ConfirmedQuantity { get; set; }

        public int RequestedLargeQuantity { get; set; }
        public int ApprovedLargeQuantity { get; set; }
        public int ConfirmedLargeQuantity { get; set; }

        public string SmallUnit { get; set; } = "قطعة";
        public string LargeUnit { get; set; } = "كرتونة";
        public int UnitsPerLargeUnit { get; set; } = 1;

        public string? Notes { get; set; }

        public LoadRequest LoadRequest { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}