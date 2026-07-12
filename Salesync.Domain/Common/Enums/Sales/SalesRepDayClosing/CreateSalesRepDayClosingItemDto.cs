namespace Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing
{
    public class CreateSalesRepDayClosingItemDto
    {
        public int ProductId { get; set; }

        public int ActualReturnedQuantity { get; set; }

        public string? Notes { get; set; }
    }
}