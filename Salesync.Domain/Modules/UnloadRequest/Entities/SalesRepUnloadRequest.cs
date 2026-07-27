using Salesync.Domain.Common;
using Salesync.Domain.Common.Enums.UnloadRequest;

namespace Salesync.Domain.Modules.UnloadRequest.Entities
{
    public class SalesRepUnloadRequest : BaseEntity
    {
        // Document
        public string RequestNumber { get; set; } = string.Empty;

        // Core relations
        public int SalesRepId { get; set; }
        public int SalesRepSessionId { get; set; }
        public int WarehouseId { get; set; }
        public int? BranchId { get; set; }

        // Status
        public UnloadRequestStatus Status { get; set; } = UnloadRequestStatus.Draft;

        // Dates
        public DateTime RequestedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        // Users audit
        public string? RequestedByUserId { get; set; }
        public string? ConfirmedByUserId { get; set; }
        public string? CancelledByUserId { get; set; }

        // Totals - small unit
        public int TotalRequestedQuantity { get; set; }
        public int TotalConfirmedQuantity { get; set; }
        public int TotalVarianceQuantity { get; set; }

        // Count
        public int TotalItems { get; set; }

        // Notes
        public string? SalesRepNotes { get; set; }
        public string? WarehouseNotes { get; set; }
        public string? CancellationReason { get; set; }

        // Navigation
        public ICollection<SalesRepUnloadRequestItem> Items { get; set; }
            = new List<SalesRepUnloadRequestItem>();
    }
}