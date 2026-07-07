using Salesync.Domain.Common;
using Salesync.Domain.Common.Enums.Inventory;
using Salesync.Domain.Modules.MasterData.Entities;

namespace Salesync.Domain.Modules.Inventory.Entities
{
    public class StockMovement : BaseEntity
    {
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public int Quantity { get; set; }
        public StockMovementType MovementType { get; set; }
        public StockMovementSource Source { get; set; }
        public int? SourceId { get; set; }
        public string? SourceNumber { get; set; }
        public DateTime MovementDate { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }

        public Product Product { get; set; } = null!;
        public Warehouse Warehouse { get; set; } = null!;
    }
}
