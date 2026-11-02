namespace Salesync.Application.Modules.Treasury.Dtos
{
    public class SetCashBoxOpeningBalanceDto
    {
        public decimal Amount { get; set; }

        public DateTime EffectiveDate { get; set; }

        public string? ReferenceNumber { get; set; }

        public string? Notes { get; set; }
    }
}