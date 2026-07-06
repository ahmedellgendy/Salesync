namespace Salesync.Application.Modules.Sales.Dtos.InvoiceReturn
{
    public class CreateInvoiceReturnItemDto
    {
        public int InvoiceItemId { get; set; }
        public int Quantity { get; set; }
        public string? Notes { get; set; }

    }
}
