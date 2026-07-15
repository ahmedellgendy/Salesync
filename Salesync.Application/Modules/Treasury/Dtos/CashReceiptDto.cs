namespace Salesync.Application.Modules.Treasury.Dtos
{
    public class CashReceiptDto
    {
        public int Id { get; set; }

        public string ReceiptNumber { get; set; } = string.Empty;

        public int CashBoxId { get; set; }

        public int SalesRepId { get; set; }

        public int SalesRepSessionId { get; set; }

        public int SalesRepDayClosingId { get; set; }

        public decimal ExpectedAmount { get; set; }

        public decimal ReceivedAmount { get; set; }

        public decimal VarianceAmount { get; set; }

        public string? ReceivedByUserId { get; set; }

        public DateTime ReceivedAt { get; set; }

        public string? Notes { get; set; }

        public bool IsActive { get; set; }
    }
}