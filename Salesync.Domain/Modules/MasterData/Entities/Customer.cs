using Salesync.Domain.Common;
using Salesync.Domain.Common.Enums;

namespace Salesync.Domain.Modules.MasterData.Entities
{
    public class Customer : BaseEntity
    {
        // Basic informations
        public required string Name { get; set; }
        public required string Phone { get; set; }
        public required string Email { get; set; }


        // Customer Types
        public CustomerType Type { get; set; }
        public string? CompanyName { get; set; }

        // Address Information
        public required string Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }

        // Business Information
        public decimal CreditLimit { get; set; } 
        public decimal CurrentBalance { get; set; } 
        public CustomerStatus Status { get; set; } = CustomerStatus.Pending;


        // Tracking
        public DateTime? LastPurchaseDate { get; set; }
        public decimal TotalPurchaseAmount { get; set; }
        public int TotalOrders { get; set; }


        // Relations
        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }



        
    }
}
