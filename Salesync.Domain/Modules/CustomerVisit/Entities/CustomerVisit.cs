using Salesync.Domain.Common;
using Salesync.Domain.Common.Enums.CustomerVisit;
using Salesync.Domain.Modules.MasterData.Entities;
using Salesync.Domain.Modules.Sales.Entities;
using Salesync.Domain.Modules.SalesRep.Entities;
using SalesRepEntity = Salesync.Domain.Modules.SalesRep.Entities.SalesRep;

namespace Salesync.Domain.Modules.CustomerVisit.Entities
{
    public class CustomerVisit : BaseEntity
    {
        public int SalesRepId { get; set; }
        public int CustomerId { get; set; }
        public int? RouteId { get; set; }
        public int? SalesRepSessionId { get; set; }
        public DateTime VisitDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }
        public VisitType? VisitType { get; set; }
        public VisitStatus Status { get; set; } = VisitStatus.Completed;
        public NegativeVisitReason? NegativeReason { get; set; }
        public int? InvoiceId { get; set; }
        public int? PaymentId { get; set; }
        public int? InvoiceReturnId { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Notes { get; set; }

        public SalesRepEntity SalesRep { get; set; } = null!;
        public Customer Customer { get; set; } = null!;
        public Route? Route { get; set; }
        public SalesRepSession? SalesRepSession { get; set; }

        public Invoice? Invoice { get; set; }
        public Payment? Payment { get; set; }
        public InvoiceReturn? InvoiceReturn { get; set; }
    }
}
