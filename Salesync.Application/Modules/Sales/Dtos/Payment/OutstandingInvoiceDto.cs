namespace Salesync.Application.Modules.Sales.Dtos.Payment
{
    public class OutstandingInvoiceDto
    {
        public int InvoiceId { get; set; }

        public string InvoiceNumber { get; set; } =
            string.Empty;

        public int CustomerId { get; set; }

        public string CustomerName { get; set; } =
            string.Empty;

        public DateTime InvoiceDate { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal OutstandingAmount { get; set; }

        public int OriginalSalesRepSessionId { get; set; }
    }
}