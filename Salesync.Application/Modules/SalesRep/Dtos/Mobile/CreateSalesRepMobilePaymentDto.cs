namespace Salesync.Application.Modules.SalesRep.Dtos.Mobile
{
    public class CreateSalesRepMobilePaymentDto
    {
        public int InvoiceId { get; set; }

        public decimal Amount { get; set; }

        public int PaymentMethod { get; set; } = 1;

        public string? Notes { get; set; }
    }
}