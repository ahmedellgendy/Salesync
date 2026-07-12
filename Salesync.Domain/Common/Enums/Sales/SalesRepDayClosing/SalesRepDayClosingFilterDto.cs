namespace Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing
{
    public class SalesRepDayClosingFilterDto
    {
        public int? SalesRepId { get; set; }

        public int? SalesRepSessionId { get; set; }

        public int? WarehouseId { get; set; }

        public SalesRepDayClosingStatus? Status { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }
}