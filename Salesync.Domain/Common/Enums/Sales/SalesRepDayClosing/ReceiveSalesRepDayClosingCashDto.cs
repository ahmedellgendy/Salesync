namespace Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing
{
    public class ReceiveSalesRepDayClosingCashDto
    {
        public int? CashBoxId { get; set; }

        public decimal ActualCashAmount { get; set; }

        public string? Notes { get; set; }
    }
}