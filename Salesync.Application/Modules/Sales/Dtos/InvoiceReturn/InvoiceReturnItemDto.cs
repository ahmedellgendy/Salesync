using Salesync.Domain.Common.Enums.Sales;

namespace Salesync.Application.Modules.Sales.Dtos.InvoiceReturn
{
    public class InvoiceReturnItemDto
    {
        public int Id { get; set; }
        public int InvoiceItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int BonusQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
        public ReturnCondition Condition { get; set; }
        public string? Notes { get; set; }
    }
}