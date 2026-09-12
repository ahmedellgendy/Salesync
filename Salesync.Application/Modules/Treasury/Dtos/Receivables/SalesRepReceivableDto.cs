namespace Salesync.Application.Modules.Treasury.Dtos.Receivables
{
    public class SalesRepReceivableDto
    {
        public int SalesRepId { get; set; }

        public string SalesRepCode { get; set; } =
            string.Empty;

        public string SalesRepName { get; set; } =
            string.Empty;

        public decimal LedgerBalance { get; set; }

        public decimal DebtAmount { get; set; }

        public decimal SurplusAmount { get; set; }
    }
}