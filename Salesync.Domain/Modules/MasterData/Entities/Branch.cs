    using Salesync.Domain.Common;

namespace Salesync.Domain.Modules.MasterData.Entities
{
    public class Branch : BaseEntity
    {
        public required string BranchCode { get; set; }
        public required string Name { get; set; }
        public string? City { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }

        // Navigation Properties
        public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
    }
}
