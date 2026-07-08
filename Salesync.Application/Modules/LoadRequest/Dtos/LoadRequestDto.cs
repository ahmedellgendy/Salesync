using Salesync.Domain.Common.Enums.LoadRequest;

namespace Salesync.Application.Modules.LoadRequest.Dtos
{
    public class LoadRequestDto
    {
        public int Id { get; set; }
        public string LoadRequestNumber { get; set; } = string.Empty;
        public int SalesRepId { get; set; }
        public string? SalesRepName { get; set; }
        public int WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public DateTime RequestDate { get; set; }
        public LoadRequestStatus Status { get; set; }
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

        public List<LoadRequestItemDto> Items { get; set; } = new();
    }
}