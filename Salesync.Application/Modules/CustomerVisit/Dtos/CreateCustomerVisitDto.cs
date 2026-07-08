using Salesync.Domain.Common.Enums.CustomerVisit;

namespace Salesync.Application.Modules.CustomerVisit.Dtos
{
    public class CreateCustomerVisitDto
    {
        public int? SalesRepId { get; set; }
        public int CustomerId { get; set; }
        public int? RouteId { get; set; }
        public int? SalesRepSessionId { get; set; }
        public VisitType VisitType { get; set; }
        public NegativeVisitReason? NegativeReason { get; set; }
        public int? InvoiceId { get; set; }
        public int? PaymentId { get; set; }
        public int? InvoiceReturnId { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Notes { get; set; }
    }
}
