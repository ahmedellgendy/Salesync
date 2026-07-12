namespace Salesync.Application.Modules.CustomerVisit.Dtos
{
    public class StartCustomerVisitDto
    {
        public int? SalesRepId { get; set; }

        public int CustomerId { get; set; }

        public int? RouteId { get; set; }

        public int SalesRepSessionId { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public string? Notes { get; set; }
    }
}