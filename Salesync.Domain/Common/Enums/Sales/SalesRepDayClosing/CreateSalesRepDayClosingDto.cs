namespace Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing
{
    public class CreateSalesRepDayClosingDto
    {
        public int SalesRepSessionId { get; set; }

        public int WarehouseId { get; set; }

        public string? Notes { get; set; }

    }
}