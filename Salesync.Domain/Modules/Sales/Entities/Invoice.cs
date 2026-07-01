using Salesync.Domain.Common;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Modules.MasterData.Entities;
using SalesRepEntity = Salesync.Domain.Modules.SalesRep.Entities.SalesRep;

namespace Salesync.Domain.Modules.Sales.Entities
{
    public class Invoice : BaseEntity
    {
        public required string InvoiceNumber { get; set; }
        public int CustomerId { get; set; }
        public int WarehouseId { get; set; }
        public SalesChannel SalesChannel { get; set; }
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; } 
        public int? SalesRepId { get; set; }
        public int? SalesRepSessionId { get; set; }
        public string? Notes { get; set; }

        // Navigation Properties
        public Customer Customer { get; set; } = null!;
        public Warehouse Warehouse { get; set; } = null!;
   
        public SalesRepEntity? SalesRep { get; set; }
        public SalesRepSession? SalesRepSession { get; set; }

        public ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<InvoiceReturn> InvoiceReturns { get; set; } = new List<InvoiceReturn>();
    }
}

