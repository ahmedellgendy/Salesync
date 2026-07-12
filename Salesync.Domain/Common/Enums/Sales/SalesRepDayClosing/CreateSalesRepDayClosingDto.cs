namespace Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing
{
    public class CreateSalesRepDayClosingDto
    {
        public int SalesRepSessionId { get; set; }

        public int WarehouseId { get; set; }

        public decimal ActualCashAmount { get; set; }

        public string? Notes { get; set; }

        public List<CreateSalesRepDayClosingItemDto> Items { get; set; } = new();
    }
}