using Salesync.Domain.Common.Enums.UnloadRequest;

namespace Salesync.Application.Modules.UnloadRequest.Dtos
{
    public class SalesRepUnloadRequestDto
    {
        public int Id { get; set; }

        public string RequestNumber { get; set; } = string.Empty;

        public int SalesRepId { get; set; }
        public string? SalesRepCode { get; set; }
        public string? SalesRepName { get; set; }

        public int SalesRepSessionId { get; set; }

        public int WarehouseId { get; set; }
        public string? WarehouseName { get; set; }

        public int? BranchId { get; set; }

        public UnloadRequestStatus Status { get; set; }
        public string StatusName => Status.ToString();

        public DateTime RequestedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        public string? RequestedByUserId { get; set; }
        public string? ConfirmedByUserId { get; set; }
        public string? CancelledByUserId { get; set; }

        public int TotalRequestedQuantity { get; set; }
        public int TotalConfirmedQuantity { get; set; }
        public int TotalVarianceQuantity { get; set; }

        public int TotalItems { get; set; }

        public string? SalesRepNotes { get; set; }
        public string? WarehouseNotes { get; set; }
        public string? CancellationReason { get; set; }

        public List<SalesRepUnloadRequestItemDto> Items { get; set; } = new();
    }
}