using Salesync.Domain.Common;
using Salesync.Domain.Common.Enums.Treasury;
using SalesRepEntity = Salesync.Domain.Modules.SalesRep.Entities.SalesRep;

namespace Salesync.Domain.Modules.Treasury.Entities
{
    public class SalesRepCashLedger : BaseEntity
    {
        public int SalesRepId { get; set; }
        public DateTime EntryDate { get; set; } = DateTime.UtcNow;
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public SalesRepCashLedgerSource Source { get; set; }
        public int? CashReceiptId { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }

        public SalesRepEntity SalesRep { get; set; } = null!;
        public CashReceipt? CashReceipt { get; set; }
    }
}