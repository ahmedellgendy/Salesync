using Salesync.Domain.Common.Enums.Sales;

namespace Salesync.Application.Modules.Reports.Admin.Dtos
{
    public class AdminInvoiceReportItemDto
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;

        public DateTime InvoiceDate { get; set; }

        public int SalesRepId { get; set; }
        public string SalesRepName { get; set; } = string.Empty;

        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;

        public InvoiceStatus Status { get; set; }
        public PaymentStatus PaymentStatus { get; set; }

        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
    }
}