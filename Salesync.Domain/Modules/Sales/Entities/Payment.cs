using Salesync.Domain.Common;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Modules.MasterData.Entities;
using SalesRepEntity = Salesync.Domain.Modules.SalesRep.Entities.SalesRep;

namespace Salesync.Domain.Modules.Sales.Entities
{
    public class Payment : BaseEntity
    {
        public required string PaymentNumber { get; set; }
        public int InvoiceId { get; set; }
        public int CustomerId { get; set; } 
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public DateTime PaymentDate { get; set; }
        public int? SalesRepId { get; set; }
        public int? SalesRepSessionId { get; set; }
        public string? CheckNumber { get; set; }
        public string? TransactionReference { get; set; }
        public DateTime? CheckDueDate { get; set; }
        public string? BankName { get; set; }
        public string? Notes { get; set; }

        // Navigation Properties
        public Invoice Invoice { get; set; } = null!;
        public Customer Customer { get; set; } = null!;
        public SalesRepEntity? SalesRep { get; set; }
        public SalesRepSession? SalesRepSession { get; set; }
    }
}
