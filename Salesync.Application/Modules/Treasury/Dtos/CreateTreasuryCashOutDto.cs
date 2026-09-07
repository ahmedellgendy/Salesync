using Salesync.Domain.Common.Enums.Treasury;

namespace Salesync.Application.Modules.Treasury.Dtos
{
    public class CreateTreasuryCashOutDto
    {
        public int CashBoxId { get; set; }

        public decimal Amount { get; set; }

        public TreasuryTransactionSource Source { get; set; }

        public string? ReferenceNumber { get; set; }

        public string? Notes { get; set; }
    }
}