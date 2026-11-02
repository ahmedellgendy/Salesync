using Salesync.Domain.Common;
using Salesync.Domain.Modules.MasterData.Entities;
using Rep = Salesync.Domain.Modules.SalesRep.Entities.SalesRep;
namespace Salesync.Domain.Modules.Sales.Entities;

// Posted receiving/credit document. No public update, delete or cash-refund command.
public class InvoiceReturnReceipt : BaseEntity
{
    public Guid OperationId { get; set; }
    public required string RequestHash { get; set; }
    public required string ReceiptNumber { get; set; }
    public required string CreditNoteNumber { get; set; }
    public int InvoiceReturnId { get; set; }
    public int InvoiceId { get; set; }
    public int CustomerId { get; set; }
    public int? SalesRepId { get; set; }
    public int? SalesRepSessionId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AppliedToInvoiceAmount { get; set; }
    public decimal CustomerCreditAmount { get; set; }
    public bool CloseRemaining { get; set; }
    public string? Notes { get; set; }
    public InvoiceReturn InvoiceReturn { get; set; } = null!;
    public Invoice Invoice { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public Rep? SalesRep { get; set; }
    public ICollection<InvoiceReturnReceiptItem> Items { get; set; } = new List<InvoiceReturnReceiptItem>();
}
