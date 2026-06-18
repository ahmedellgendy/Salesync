namespace Salesync.Application.Modules.SalesRep.Dtos.RouteDto
{
    public class CreateRouteDto
    {
        public required string RouteCode { get; set; }
        public required string Name { get; set; }
        public int BranchId { get; set; }
        public int? BusinessUnitId { get; set; }
    }
}
