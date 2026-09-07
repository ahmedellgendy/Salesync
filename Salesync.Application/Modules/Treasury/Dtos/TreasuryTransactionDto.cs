namespace Salesync.Application.Modules.Treasury.Dtos
{
    public class TreasuryTransactionDto
    {
        public int Id { get; set; }

        public int CashBoxId { get; set; }

        public int Type { get; set; }

        public int Source { get; set; }

        public decimal Amount { get; set; }

        public decimal BalanceBefore { get; set; }

        public decimal BalanceAfter { get; set; }

        public DateTime TransactionDate { get; set; }

        public string? ReferenceNumber { get; set; }

        public int? CashReceiptId { get; set; }

        public string? Notes { get; set; }

        public string? CreatedByUserId { get; set; }
    }
}