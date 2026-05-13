namespace Salesync.Domain.Entities
{
    public class Warehouse : BaseEntity
    {
        public int BranchId { get; set; }
        public required string Name { get; set; }
        public required string Location { get; set; }
        
        // Navigation Properties
        public Branch Branch { get; set; } = null!;

        public ICollection<Product> Products { get; set; } = new List<Product>();

    }
}
