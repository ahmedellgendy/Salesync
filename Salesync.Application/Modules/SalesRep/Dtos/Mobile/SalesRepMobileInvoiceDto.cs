namespace Salesync.Application.Modules.SalesRep.Dtos.Mobile
{
    public class SalesRepMobileInvoiceDto
    {
        public int Id { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;

        public int CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public int WarehouseId { get; set; }

        public int? SalesRepSessionId { get; set; }

        public int Status { get; set; }

        public int PaymentStatus { get; set; }

        public decimal SubTotal { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal RemainingAmount { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? Notes { get; set; }
    }
}