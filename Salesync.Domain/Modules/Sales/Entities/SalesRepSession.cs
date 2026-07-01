using Salesync.Domain.Common;
using Salesync.Domain.Common.Enums.Sales;
using SalesRepEntity = Salesync.Domain.Modules.SalesRep.Entities.SalesRep;

namespace Salesync.Domain.Modules.Sales.Entities
{
    public class SalesRepSession : BaseEntity
    {
        public int SalesRepId { get; set; }
        public SalesRepEntity SalesRep { get; set; } = null!;

        public DateTime WorkingDate { get; set; }
        public DayStatus Status { get; set; } = DayStatus.NotStarted;

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public decimal GrossSales { get; set; }
        public decimal NetSales { get; set; }
        public decimal TotalCollection { get; set; }
        public decimal TotalReturnAmount { get; set; }

        public int TotalInvoices { get; set; }
        public int TotalVisits { get; set; }

        public string? Notes { get; set; }


        // Navigation Properties
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<InvoiceReturn> Returns { get; set; } = new List<InvoiceReturn>();
    }
}
