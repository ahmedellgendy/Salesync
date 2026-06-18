using Salesync.Domain.Common;
using Salesync.Domain.Modules.MasterData.Entities;

namespace Salesync.Domain.Modules.SalesRep.Entities
{
    public class Route : BaseEntity
    {
        public required string RouteCode { get; set; }
        public required string Name { get; set; }
        public int BranchId { get; set; }
        public string? Type { get; set; }
        public string? RegionCode { get; set; }
        public string? DistrictCode { get; set; }
        public string? CityCode { get; set; }
        public string? AreaCode { get; set; }
        public string? RouteChannel { get; set; }
        public string? RouteGTM { get; set; }
        public string? RouteCategory { get; set; }
        public int? BusinessUnitId { get; set; }
        public int? AssignedSalesRepId { get; set; }


        // Navigation Properties
        public Branch Branch { get; set; } = null!;
        public SalesRep? AssignedSalesRep { get; set; }
        public ICollection<RouteCustomer> RouteCustomers { get; set; } = new List<RouteCustomer>();
        public ICollection<SalesRepRoute> SalesRepRoutes { get; set; } = new List<SalesRepRoute>();

    }
}
