using Salesync.Domain.Common;
using Salesync.Domain.Common.Enums.LoadRequest;
using Salesync.Domain.Modules.MasterData.Entities;
using SalesRepEntity = Salesync.Domain.Modules.SalesRep.Entities.SalesRep;

namespace Salesync.Domain.Modules.LoadRequest.Entities
{
    public class LoadRequest : BaseEntity
    {
        public required string LoadRequestNumber { get; set; }
        public int SalesRepId { get; set; }
        public int WarehouseId { get; set; }
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public LoadRequestStatus Status { get; set; } = LoadRequestStatus.Pending;
        public string? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectedByUserId { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? RejectionReason { get; set; }
        public string? ConfirmedByUserId { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public string? CancelledByUserId { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? Notes { get; set; }

        public SalesRepEntity SalesRep { get; set; } = null!;
        public Warehouse Warehouse { get; set; } = null!;

        public ICollection<LoadRequestItem> Items { get; set; } = new List<LoadRequestItem>();
    }
}
