namespace Salesync.Application.Modules.SalesRep.Dtos.Mobile
{
    public class SalesRepMobileRouteDto
    {
        public int RouteId { get; set; }
        public string RouteCode { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public int TotalCustomers { get; set; }
        public int VisitedCustomers { get; set; }
        public int RemainingCustomers { get; set; }
        public bool HasActiveVisit { get; set; }
        public bool CanOpen { get; set; }
    }
}