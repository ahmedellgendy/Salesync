namespace Salesync.Application.Modules.Treasury.Dtos.Receivables
{
    public class SalesRepDebtPaymentResultDto
    {
        public int SalesRepId { get; set; }

        public string SalesRepName { get; set; } =
            string.Empty;

        public decimal PreviousDebtAmount { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal RemainingDebtAmount { get; set; }

        public int CashBoxId { get; set; }

        public decimal CashBoxBalanceAfter { get; set; }

        public int TreasuryTransactionId { get; set; }

        public DateTime PaymentDate { get; set; }
    }
}