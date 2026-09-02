namespace Salesync.Application.Modules.SalesRep.Dtos.RouteDto
{
    public class CreateRouteDto
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

        public int? AssignedSalesRepId { get; set; }

        public int? BusinessUnitId { get; set; }
    }
}