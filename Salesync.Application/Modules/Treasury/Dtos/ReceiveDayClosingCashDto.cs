namespace Salesync.Application.Modules.Treasury.Dtos
{
    public class ReceiveDayClosingCashDto
    {
        public int CashBoxId { get; set; }

        public decimal ReceivedAmount { get; set; }

        public string? Notes { get; set; }
    }
}