namespace Salesync.Application.Modules.Treasury.Dtos
{
    public class TreasuryCashOutDto
    {
        public int TreasuryTransactionId { get; set; }

        public int CashBoxId { get; set; }

        public decimal Amount { get; set; }

        public decimal BalanceBefore { get; set; }

        public decimal BalanceAfter { get; set; }

        public int Source { get; set; }

        public string? ReferenceNumber { get; set; }

        public string? Notes { get; set; }

        public string? CreatedByUserId { get; set; }

        public DateTime TransactionDate { get; set; }
        public int? ExpenseCategoryId { get; set; }

        public string? ExpenseCategoryName { get; set; }
    }
}