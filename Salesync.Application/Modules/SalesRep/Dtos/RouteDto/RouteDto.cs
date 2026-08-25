namespace Salesync.Application.Modules.SalesRep.Dtos.RouteDto
{
    public class RouteDto
    {
        public int Id { get; set; }

        public string RouteCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public int BranchId { get; set; }
        public string? BranchName { get; set; }

        public string? Type { get; set; }

        public string? RegionCode { get; set; }
        public string? DistrictCode { get; set; }
        public string? CityCode { get; set; }
        public string? AreaCode { get; set; }

        public string? RouteChannel { get; set; }
        public string? RouteGTM { get; set; }
        public string? RouteCategory { get; set; }

        public int? AssignedSalesRepId { get; set; }
        public string? AssignedSalesRepName { get; set; }

        public int? BusinessUnitId { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}