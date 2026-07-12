using Salesync.Domain.Common;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Modules.MasterData.Entities;
using SalesRepEntity = Salesync.Domain.Modules.SalesRep.Entities.SalesRep;


namespace Salesync.Domain.Modules.Sales.Entities
{
    public class SalesRepDayClosing : BaseEntity
    {
        public required string ClosingNumber { get; set; }
        public int SalesRepId { get; set; }
        public int SalesRepSessionId { get; set; }
        public int WarehouseId { get; set; }
        public DateTime ClosingDate { get; set; } = DateTime.UtcNow;
        public SalesRepDayClosingStatus Status { get; set; } = SalesRepDayClosingStatus.PendingApproval;

        // Sales Summary
        public decimal TotalSalesAmount { get; set; }
        public decimal TotalCollectionAmount { get; set; }
        public decimal TotalReturnAmount { get; set; }

        // Cash Settlement
        public decimal ExpectedCashAmount { get; set; }
        public decimal ActualCashAmount { get; set; }
        public decimal CashVariance { get; set; }

        // Stock Summary
        public int ExpectedTotalRemainingQuantity { get; set; }
        public int ActualTotalReturnedQuantity { get; set; }
        public int TotalVarianceQuantity { get; set; }

        // Workflow
        public string? SubmittedByUserId { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectedByUserId { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? RejectionReason { get; set; }
        public string? CancelledByUserId { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? Notes { get; set; }

        // Navigation Properties
        public SalesRepEntity SalesRep { get; set; } = null!;
        public SalesRepSession SalesRepSession { get; set; } = null!;
        public Warehouse Warehouse { get; set; } = null!;

        public ICollection<SalesRepDayClosingItem> Items { get; set; } = new List<SalesRepDayClosingItem>();
    }
}