namespace Salesync.Application.Modules.SalesRep.Dtos.Mobile
{
    public class SalesRepMobileCustomerDto
    {
        public int RouteCustomerId { get; set; }
        public int RouteId { get; set; }
        public string RouteCode { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Area { get; set; }
        public string? City { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public decimal? CurrentBalance { get; set; }
        public decimal? CreditLimit { get; set; }
        public int VisitSequence { get; set; }
        public string? VisitDays { get; set; }
        public int? VisitId { get; set; }
        public int? VisitStatus { get; set; }
        public int? VisitType { get; set; }
        public bool HasVisitToday { get; set; }
        public bool CanStartVisit { get; set; }
    }
}