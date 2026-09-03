namespace Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing
{
    public class SalesRepDayClosingDto
    {
        public int Id { get; set; }

        public string ClosingNumber { get; set; } = string.Empty;

        public int SalesRepId { get; set; }
        public string? SalesRepName { get; set; }

        public int SalesRepSessionId { get; set; }

        public int WarehouseId { get; set; }
        public string? WarehouseName { get; set; }

        public DateTime ClosingDate { get; set; }

        public SalesRepDayClosingStatus Status { get; set; }

        public decimal TotalSalesAmount { get; set; }

        public decimal TotalCollectionAmount { get; set; }

        public decimal TotalReturnAmount { get; set; }
        public decimal OutstandingAmount { get; set; }

        public decimal CashCollectionAmount { get; set; }

        public decimal NonCashCollectionAmount { get; set; }
        public decimal ExpectedCashAmount { get; set; }

        public decimal ActualCashAmount { get; set; }

        public decimal CashVariance { get; set; }

        public int ExpectedTotalRemainingQuantity { get; set; }

        public int ActualTotalReturnedQuantity { get; set; }

        public int TotalVarianceQuantity { get; set; }

        public bool IsCashReceived { get; set; }

        public string? CashReceivedByUserId { get; set; }

        public DateTime? CashReceivedAt { get; set; }

        public string? CashNotes { get; set; }

        public bool IsStockReceived { get; set; }

        public string? StockReceivedByUserId { get; set; }

        public DateTime? StockReceivedAt { get; set; }

        public string? StockNotes { get; set; }

        public string? SubmittedByUserId { get; set; }
        public DateTime? SubmittedAt { get; set; }

        public string? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public string? RejectedByUserId { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? RejectionReason { get; set; }

        public string? CancelledByUserId { get; set; }
        public DateTime? CancelledAt { get; set; }

        public string? Notes { get; set; }

        public List<SalesRepDayClosingItemDto> Items { get; set; } = new();
    }
}