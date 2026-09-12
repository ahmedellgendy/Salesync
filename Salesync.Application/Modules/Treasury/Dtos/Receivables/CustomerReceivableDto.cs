namespace Salesync.Application.Modules.Treasury.Dtos.Receivables
{
    public class CustomerReceivableDto
    {
        public int CustomerId { get; set; }

        public string CustomerName { get; set; } =
            string.Empty;

        public string? AccountNumber { get; set; }

        public int TotalInvoices { get; set; }

        public decimal TotalInvoiceAmount { get; set; }

        public decimal TotalPaidAmount { get; set; }

        public decimal OutstandingAmount { get; set; }
    }
}