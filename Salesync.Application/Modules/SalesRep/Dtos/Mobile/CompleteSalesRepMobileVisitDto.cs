using Salesync.Domain.Common.Enums.CustomerVisit;

namespace Salesync.Application.Modules.SalesRep.Dtos.Mobile
{
    public class CompleteSalesRepMobileVisitDto
    {
        public VisitType VisitType { get; set; }
        public NegativeVisitReason? NegativeReason { get; set; }
        public int? InvoiceId { get; set; }
        public int? PaymentId { get; set; }
        public int? InvoiceReturnId { get; set; }
        public string? Notes { get; set; }
    }
}