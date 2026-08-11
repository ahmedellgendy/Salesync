namespace Salesync.Application.Modules.Sales.Dtos.InvoiceReturn
{
    public class MobileReturnableInvoiceDto
    {
        public int Id { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;

        public int CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public decimal TotalAmount { get; set; }

        public int ReturnableItemsCount { get; set; }
    }
}