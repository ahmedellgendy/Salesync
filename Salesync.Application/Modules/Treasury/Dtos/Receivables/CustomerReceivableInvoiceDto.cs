namespace Salesync.Application.Modules.Treasury.Dtos.Receivables
{
    public class CustomerReceivableInvoiceDto
    {
        public int InvoiceId { get; set; }

        public string InvoiceNumber { get; set; } =
            string.Empty;

        public DateTime InvoiceDate { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal OutstandingAmount { get; set; }

        public int PaymentStatus { get; set; }

        public int? SalesRepId { get; set; }

        public string? SalesRepName { get; set; }
    }
}