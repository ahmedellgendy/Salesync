using Salesync.Domain.Common.Enums.LoadRequest;

namespace Salesync.Application.Modules.LoadRequest.Dtos
{
    public class SalesRepInventoryMovementDto
    {
        public int Id { get; set; }
        public int SalesRepId { get; set; }
        public string? SalesRepName { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ItemCode { get; set; }
        public int Quantity { get; set; }
        public SalesRepInventoryMovementType MovementType { get; set; }
        public SalesRepInventoryMovementSource Source { get; set; }
        public int? SourceId { get; set; }
        public string? SourceNumber { get; set; }
        public DateTime MovementDate { get; set; }
        public string? Notes { get; set; }
    }
}