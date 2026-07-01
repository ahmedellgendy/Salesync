using Salesync.Domain.Common.Enums.Sales;

namespace Salesync.Application.Modules.Sales.Dtos.Payment
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public string PaymentNumber { get; set; } = null!;
        public int InvoiceId { get; set; }
        public int CustomerId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? CheckNumber { get; set; }
        public string? Notes { get; set; }
    }
}
