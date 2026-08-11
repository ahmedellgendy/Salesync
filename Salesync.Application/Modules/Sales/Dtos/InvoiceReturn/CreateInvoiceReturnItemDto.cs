using Salesync.Domain.Common.Enums.Sales;

namespace Salesync.Application.Modules.Sales.Dtos.InvoiceReturn
{
    public class CreateInvoiceReturnItemDto
    {
        public int InvoiceItemId { get; set; }

        // Paid quantity
        public int Quantity { get; set; }

        // Free / bonus quantity
        public int BonusQuantity { get; set; }

        public ReturnCondition Condition { get; set; }

        public string? Notes { get; set; }
    }
}