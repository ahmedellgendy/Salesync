namespace Salesync.Application.Modules.Treasury.Dtos.Receivables
{
    public class ReceivablesSummaryDto
    {
        public decimal CustomerDebtAmount { get; set; }

        public decimal SalesRepDebtAmount { get; set; }

        public decimal SalesRepSurplusAmount { get; set; }

        public decimal TotalReceivables =>
            CustomerDebtAmount +
            SalesRepDebtAmount;

        public int CustomersWithDebt { get; set; }

        public int SalesRepsWithDebt { get; set; }
    }
}