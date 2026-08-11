using Salesync.Domain.Common.Enums.Sales;

namespace Salesync.Application.Modules.Sales.Dtos.InvoiceReturn
{
    public class CreateInvoiceReturnItemDto
    {
        public int InvoiceItemId { get; set; }
        public int Quantity { get; set; }
        public ReturnCondition Condition { get; set; }
        public string? Notes { get; set; }

    }
}
