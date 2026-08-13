using Salesync.Domain.Common;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Modules.MasterData.Entities;

namespace Salesync.Domain.Modules.Sales.Entities
{
    public class InvoiceReturnItem : BaseEntity
    {
        public int InvoiceReturnId { get; set; }
        public int InvoiceItemId { get; set; }
        public int ProductId { get; set; }
        public required string ProductName { get; set; }
        public required string ItemCode { get; set; }


        // Paid / sale quantity returned
        public int Quantity { get; set; }


        // Free / bonus quantity returned
        public int BonusQuantity { get; set; }


        // Original invoice unit price snapshot
        public decimal UnitPrice { get; set; }


        // Actual financial credit value for paid quantity only
        public decimal TotalAmount { get; set; }
        public ReturnCondition Condition { get; set; }
        public string? Notes { get; set; }


        // Navigation Properties
        public InvoiceReturn InvoiceReturn { get; set; } = null!;
        public InvoiceItem InvoiceItem { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}