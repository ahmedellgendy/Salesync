using Salesync.Domain.Common;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Modules.MasterData.Entities;
namespace Salesync.Domain.Modules.Sales.Entities;
public class InvoiceReturnReceiptItem : BaseEntity
{
    public int ReceiptId { get; set; }
    public int InvoiceReturnItemId { get; set; }
    public int ProductId { get; set; }
    public required string ProductName { get; set; }
    public required string ItemCode { get; set; }
    public int WarehouseId { get; set; }
    public int Quantity { get; set; }
    public int BonusQuantity { get; set; }
    public ReturnCondition Condition { get; set; }
    public decimal TotalAmount { get; set; }
    public InvoiceReturnReceipt Receipt { get; set; } = null!;
    public InvoiceReturnItem InvoiceReturnItem { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
