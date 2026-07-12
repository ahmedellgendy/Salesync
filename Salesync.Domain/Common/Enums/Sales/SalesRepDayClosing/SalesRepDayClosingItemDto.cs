namespace Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing
{
    public class SalesRepDayClosingItemDto
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public string? ProductName { get; set; }

        public string? ItemCode { get; set; }

        public int ExpectedRemainingQuantity { get; set; }

        public int ActualReturnedQuantity { get; set; }

        public int VarianceQuantity { get; set; }

        public string? Notes { get; set; }
    }
}