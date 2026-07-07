using Salesync.Domain.Common;
using Salesync.Domain.Modules.MasterData.Entities;

namespace Salesync.Domain.Modules.Inventory.Entities
{
    public class StockBalance : BaseEntity
    {
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public int Quantity { get; set; }
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        public Product Product { get; set; } = null!;
        public Warehouse Warehouse { get; set; } = null!;
    }
}
