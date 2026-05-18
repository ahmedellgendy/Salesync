using Salesync.Domain.Common;

namespace Salesync.Domain.Modules.MasterData.Entities
{
    public class Branch : BaseEntity
    {
        public required string Name { get; set; }
        public required string City { get; set; }
        public required string Address { get; set; }
        public required string Phone { get; set; }

        // Navigation Properties
        public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
    }
}
