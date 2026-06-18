namespace Salesync.Application.Modules.SalesRep.Dtos.RouteCustomerDto
{
    public class CreateRouteCustomerDto
    {
        public int RouteId { get; set; }
        public int CustomerId { get; set; }
        public string? VisitDays { get; set; }
        public string? Notes { get; set; }
    }
}
