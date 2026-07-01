using Salesync.Domain.Common;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Modules.MasterData.Entities;
using SalesRepEntity = Salesync.Domain.Modules.SalesRep.Entities.SalesRep;

namespace Salesync.Domain.Modules.Sales.Entities
{
    public class InvoiceReturn : BaseEntity
    {
        public required string ReturnNumber { get; set; }
        public int InvoiceId { get; set; }
        public int CustomerId { get; set; }
        public ReturnStatus Status { get; set; } = ReturnStatus.Pending;
        public ReturnReason ReturnReason { get; set; }
        public decimal TotalAmount { get; set; }
        public int? SalesRepId { get; set; }
        public string? ReasonNotes { get; set; }
        public int? SalesRepSessionId { get; set; }

        // Navigation Properties
        public Invoice Invoice { get; set; } = null!;
        public Customer Customer { get; set; } = null!;
        public SalesRepEntity? SalesRep { get; set; }
        public ICollection<InvoiceReturnItem> Items { get; set; } = new List<InvoiceReturnItem>();
        public SalesRepSession? SalesRepSession { get; set; }


    }
}
