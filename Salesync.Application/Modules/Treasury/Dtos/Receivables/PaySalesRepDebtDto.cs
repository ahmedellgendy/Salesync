namespace Salesync.Application.Modules.Treasury.Dtos.Receivables
{
    public class PaySalesRepDebtDto
    {
        public int CashBoxId { get; set; }

        public decimal Amount { get; set; }

        public string? ReferenceNumber { get; set; }

        public string? Notes { get; set; }
    }
}