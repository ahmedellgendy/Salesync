namespace Salesync.Application.Modules.SalesRep.Dtos.RouteDto
{
    public class RouteDto
    {
        public int Id { get; set; }
        public string RouteCode { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int BranchId { get; set; }
        public int? BusinessUnitId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
