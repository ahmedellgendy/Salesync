namespace Salesync.Application.Modules.Inventory.Dtos
{
    public class StockBalanceDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public int Quantity { get; set; }

        public DateTime LastUpdatedAt { get; set; }
    }
}
