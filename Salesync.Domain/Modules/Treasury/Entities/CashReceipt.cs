using Salesync.Domain.Common;
using Salesync.Domain.Modules.Sales.Entities;
using SalesRepEntity = Salesync.Domain.Modules.SalesRep.Entities.SalesRep;


namespace Salesync.Domain.Modules.Treasury.Entities
{
    public class CashReceipt : BaseEntity
    {
        public required string ReceiptNumber { get; set; }
        public int CashBoxId { get; set; }
        public int SalesRepId { get; set; }
        public int SalesRepSessionId { get; set; }
        public int SalesRepDayClosingId { get; set; }
        public decimal ExpectedAmount { get; set; }
        public decimal ReceivedAmount { get; set; }
        public decimal VarianceAmount { get; set; }
        public string? ReceivedByUserId { get; set; }
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }

        public CashBox CashBox { get; set; } = null!;
        public SalesRepEntity SalesRep { get; set; } = null!;
        public SalesRepSession SalesRepSession { get; set; } = null!;
        public SalesRepDayClosing SalesRepDayClosing { get; set; } = null!;
    }
}