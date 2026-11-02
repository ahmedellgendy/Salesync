using Salesync.Domain.Common.Enums.Sales;
namespace Salesync.Application.Modules.Sales.Dtos.InvoiceReturn;
public sealed class ReceiveInvoiceReturnDto
{
    public Guid OperationId { get; set; }
    public bool CloseRemaining { get; set; }
    public string? Notes { get; set; }
    public List<ReceiveInvoiceReturnItemDto> Items { get; set; } = [];
}
public sealed class ReceiveInvoiceReturnItemDto
{
    public int InvoiceReturnItemId { get; set; }
    public int WarehouseId { get; set; }
    public int Quantity { get; set; }
    public int BonusQuantity { get; set; }
}
public sealed class ReturnReceivingLineDto
{
    public int Id { get; set; }
    public string ProductName { get; set; } = "";
    public ReturnCondition Condition { get; set; }
    public int Quantity { get; set; }
    public int BonusQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public int ReceivedBonusQuantity { get; set; }
    public decimal ReceivedCreditAmount { get; set; }
}
public sealed class ReturnReceiptDto
{
    public int Id { get; set; }
    public Guid OperationId { get; set; }
    public string ReceiptNumber { get; set; } = "";
    public string CreditNoteNumber { get; set; } = "";
    public DateTime PostedAt { get; set; }
    public int? PostingSessionId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AppliedToInvoiceAmount { get; set; }
    public decimal CustomerCreditAmount { get; set; }
    public bool CloseRemaining { get; set; }
    public string? Notes { get; set; }
}
public sealed class ReturnReceivingDto
{
    public int ReturnId { get; set; }
    public string ReturnNumber { get; set; } = "";
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string InvoiceNumber { get; set; } = "";
    public ReturnStatus Status { get; set; }
    public decimal InvoiceRemainingAmount { get; set; }
    public decimal TotalReceivedCreditAmount { get; set; }
    public decimal TotalAppliedToInvoiceAmount { get; set; }
    public decimal CustomerCreditBalance { get; set; }
    public List<ReturnReceivingLineDto> Items { get; set; } = [];
    public List<ReturnReceiptDto> Receipts { get; set; } = [];
}
