using Salesync.Domain.Common.Enums.Sales;

namespace Salesync.Application.Modules.Reports.Admin.Dtos
{
    public class AdminPaymentReportItemDto
    {
        public int PaymentId { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;

        public DateTime PaymentDate { get; set; }

        public int SalesRepId { get; set; }
        public string SalesRepName { get; set; } = string.Empty;

        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;

        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus Status { get; set; }

        public string? CheckNumber { get; set; }
        public DateTime? CheckDueDate { get; set; }
        public string? BankName { get; set; }
        public string? TransactionReference { get; set; }
    }
}