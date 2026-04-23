namespace Salesync.Domain.Entities
{
    public class Branch
    {
        public int BranchId { get; set; }
        public required string Name { get; set; }
        public required string City { get; set; }
        public required string Address { get; set; }
        public required string Phone { get; set; }
        public bool IsActive { get; set; }

        public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
    }
}
