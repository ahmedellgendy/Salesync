using Salesync.Domain.Common;
using Salesync.Domain.Common.Enums.MasterData;

namespace Salesync.Domain.Modules.MasterData.Entities
{
    public class Warehouse : BaseEntity
    {
        public required string WarehouseCode { get; set; }
        public required string Name { get; set; }
        public int BranchId { get; set; }
        public string? Location { get; set; }
        public WarehouseType Type { get; set; } = WarehouseType.Main;

        // GPS Location
        public decimal? Latitude { get; set; }                 
        public decimal? Longitude { get; set; }                 
        public decimal? ErrorRadius { get; set; }               

        // Navigation Properties
        public Branch Branch { get; set; } = null!;
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
