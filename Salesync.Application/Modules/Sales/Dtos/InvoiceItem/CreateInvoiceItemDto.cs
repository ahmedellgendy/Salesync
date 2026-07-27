namespace Salesync.Application.Modules.Sales.Dtos.InvoiceItem
{
    public class CreateInvoiceItemDto
    {
        public int ProductId { get; set; }

        public int SaleLargeQuantity { get; set; }

        public int BonusLargeQuantity { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal DiscountPercentage { get; set; }
    }
}