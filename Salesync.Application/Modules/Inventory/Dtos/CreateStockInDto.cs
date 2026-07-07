namespace Salesync.Application.Modules.Inventory.Dtos
{
    public class CreateStockInDto
    {
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public int Quantity { get; set; }
        public string? Notes { get; set; }
    }
}
