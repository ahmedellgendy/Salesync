using Salesync.Domain.Common.Enums.CustomerVisit;

namespace Salesync.Application.Modules.Reports.Admin.Dtos
{
    public class AdminVisitReportItemDto
    {
        public int VisitId { get; set; }

        public DateTime VisitDate { get; set; }
        public DateTime? EndTime { get; set; }

        public int SalesRepId { get; set; }
        public string SalesRepName { get; set; } = string.Empty;

        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;

        public int? RouteId { get; set; }

        public VisitType? VisitType { get; set; }
        public VisitStatus Status { get; set; }
        public NegativeVisitReason? NegativeReason { get; set; }

        public int? InvoiceId { get; set; }
        public int? PaymentId { get; set; }
        public int? InvoiceReturnId { get; set; }

        public string? Notes { get; set; }
    }
}