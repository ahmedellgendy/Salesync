namespace Salesync.Application.Modules.Sales.Models
{
    public class SalesRepDayClosingCalculationResult
    {
        public decimal TotalSalesAmount { get; set; }

        public decimal TotalCollectionAmount { get; set; }

        public decimal CashCollectionAmount { get; set; }

        public decimal NonCashCollectionAmount { get; set; }

        public decimal OutstandingAmount { get; set; }

        public decimal TotalReturnAmount { get; set; }

        public decimal ExpectedCashAmount { get; set; }

        public decimal ActualCashAmount { get; set; }

        public decimal CashVariance { get; set; }

        public int ExpectedTotalRemainingQuantity { get; set; }

        public int ActualTotalReturnedQuantity { get; set; }

        public int TotalVarianceQuantity { get; set; }

        public List<SalesRepDayClosingItemCalculation> Items { get; set; } = new();
    }
}