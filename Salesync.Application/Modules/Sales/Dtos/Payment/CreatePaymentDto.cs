using Salesync.Domain.Common.Enums.Sales;

namespace Salesync.Application.Modules.Sales.Dtos.Payment
{
    public class CreatePaymentDto
    {
        public int InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public int? SalesRepId { get; set; }
        public int? SalesRepSessionId { get; set; }
        public string? CheckNumber { get; set; }
        public DateTime? CheckDueDate { get; set; }
        public string? BankName { get; set; }
        public string? TransactionReference { get; set; }
        public string? Notes { get; set; }
    }
}
