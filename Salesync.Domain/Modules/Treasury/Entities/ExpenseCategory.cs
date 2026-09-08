using Salesync.Domain.Common;

namespace Salesync.Domain.Modules.Treasury.Entities
{
    public class ExpenseCategory : BaseEntity
    {
        public required string Code { get; set; }

        public required string Name { get; set; }

        public string? Description { get; set; }
        public ICollection<TreasuryTransaction> TreasuryTransactions { get; set; } = new List<TreasuryTransaction>();
    }
}