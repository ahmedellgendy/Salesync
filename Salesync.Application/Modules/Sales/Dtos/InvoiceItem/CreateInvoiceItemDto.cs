namespace Salesync.Application.Modules.Sales.Dtos.InvoiceItem
{
    public class CreateInvoiceItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int BonusQuantity { get; set; }
        public decimal DiscountPercentage { get; set; }
    }
}
