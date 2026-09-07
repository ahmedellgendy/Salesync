using Salesync.Domain.Common;
using Salesync.Domain.Common.Enums.Treasury;

namespace Salesync.Domain.Modules.Treasury.Entities
{
    public class TreasuryTransaction : BaseEntity
    {
        public int CashBoxId { get; set; }

        public TreasuryTransactionType Type { get; set; }

        public TreasuryTransactionSource Source { get; set; }

        public decimal Amount { get; set; }

        public decimal BalanceBefore { get; set; }

        public decimal BalanceAfter { get; set; }

        public DateTime TransactionDate { get; set; }

        public string? ReferenceNumber { get; set; }

        public int? CashReceiptId { get; set; }

        public string? Notes { get; set; }

        public string? CreatedByUserId { get; set; }

        public CashBox CashBox { get; set; } = null!;

        public CashReceipt? CashReceipt { get; set; }
        public int? ExpenseCategoryId { get; set; }

        public ExpenseCategory? ExpenseCategory { get; set; }
    }
}