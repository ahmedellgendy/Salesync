namespace Salesync.Application.Modules.Sales.Models
{
    public class SalesRepDayClosingItemCalculation
    {
        public int ProductId { get; set; }

        public int ExpectedRemainingQuantity { get; set; }

        public int ActualReturnedQuantity { get; set; }

        public int VarianceQuantity { get; set; }

        public string? Notes { get; set; }
    }
}
