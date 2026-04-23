namespace Salesync.Domain.Entities
{
    public class Warehouse
    {
        public int WarehouseId { get; set; }
        public int BranchId { get; set; }
        public required string Name { get; set; }
        public required string Location { get; set; }

        public Branch Branch { get; set; } = null!;
    }
}
