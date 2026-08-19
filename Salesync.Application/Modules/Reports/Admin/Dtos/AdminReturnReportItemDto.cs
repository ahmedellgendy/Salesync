using Salesync.Domain.Common.Enums.Sales;

namespace Salesync.Application.Modules.Reports.Admin.Dtos
{
    public class AdminReturnReportItemDto
    {
        public int ReturnId { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;

        public DateTime ReturnDate { get; set; }

        public int SalesRepId { get; set; }
        public string SalesRepName { get; set; } = string.Empty;

        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;

        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;

        public ReturnReason ReturnReason { get; set; }
        public ReturnStatus Status { get; set; }

        public decimal TotalAmount { get; set; }

        public string? ReasonNotes { get; set; }
    }
}