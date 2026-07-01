using Salesync.Application.Modules.Sales.Dtos.InvoiceItem;
using Salesync.Domain.Common.Enums.Sales;

namespace Salesync.Application.Modules.Sales.Dtos.Invoice
{
    public class CreateInvoiceDto
    {
        public int CustomerId { get; set; }
        public int WarehouseId { get; set; }
        public int? SalesRepId { get; set; }
        public int? SalesRepSessionId { get; set; }
        public SalesChannel SalesChannel { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? Notes { get; set; }
        public List<CreateInvoiceItemDto> Items { get; set; } = new();

    }
}
