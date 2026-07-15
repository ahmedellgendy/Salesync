using Salesync.Domain.Common.Enums.Treasury;

namespace Salesync.Application.Modules.Treasury.Dtos
{
    public class SalesRepCashLedgerDto
    {
        public int Id { get; set; }

        public int SalesRepId { get; set; }

        public DateTime EntryDate { get; set; }

        public decimal Amount { get; set; }

        public decimal BalanceAfter { get; set; }

        public SalesRepCashLedgerSource Source { get; set; }

        public int? CashReceiptId { get; set; }

        public string? ReferenceNumber { get; set; }

        public string? Notes { get; set; }

        public bool IsActive { get; set; }
    }
}