using Salesync.Domain.Common.Enums.Inventory;

namespace Salesync.Application.Modules.Inventory.Dtos
{
    public class StockMovementDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public int Quantity { get; set; }
        public StockMovementType MovementType { get; set; }
        public StockMovementSource Source { get; set; }
        public int? SourceId { get; set; }
        public string? SourceNumber { get; set; }
        public DateTime MovementDate { get; set; }
        public string? Notes { get; set; }
    }
}
