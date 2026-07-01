using Salesync.Domain.Common;
using Salesync.Domain.Modules.MasterData.Entities;

namespace Salesync.Domain.Modules.Sales.Entities
{
    public class InvoiceReturnItem : BaseEntity
    {
        public int InvoiceReturnId { get; set; }
        public int ProductId { get; set; }
        public required string ProductName { get; set; }
        public required string ItemCode { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }

        // Navigation Properties
        public InvoiceReturn InvoiceReturn { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
