using Salesync.Domain.Common;
using Salesync.Domain.Modules.MasterData.Entities;

namespace Salesync.Domain.Modules.Treasury.Entities
{
    public class CashBox : BaseEntity
    {
        public required string Code { get; set; }
        public required string Name { get; set; }
        public int? BranchId { get; set; }
        public string Currency { get; set; } = "EGP";
        public decimal CurrentBalance { get; set; }
        public string? Notes { get; set; }

        public Branch? Branch { get; set; }
        public ICollection<CashReceipt> CashReceipts { get; set; } = new List<CashReceipt>();
    }
}