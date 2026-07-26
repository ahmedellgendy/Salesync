namespace Salesync.Application.Modules.SalesRep.Dtos.Mobile
{
    public class CreateSalesRepMobileInvoiceDto
    {
        public int VisitId { get; set; }
        public int CustomerId { get; set; }
        public int SalesRepSessionId { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? Notes { get; set; }
        public List<CreateSalesRepMobileInvoiceItemDto> Items { get; set; } = new();
    }

    public class CreateSalesRepMobileInvoiceItemDto
    {
        public int ProductId { get; set; }
        public int SaleLargeQuantity { get; set; }
        public int BonusLargeQuantity { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercentage { get; set; }
    }
}