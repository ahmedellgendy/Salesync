using Salesync.Domain.Common.Enums;

namespace Salesync.Application.Modules.MasterData.Dtos.CustomerDto
{
    public class UpdateCustomerDto
    {
        public int Id { get; set; }

        // Basic Information
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }

        // Address Information
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }

        // Customer Type
        public CustomerType? Type { get; set; }
        public string? CompanyName { get; set; }

        // Business Information
        public decimal? CreditLimit { get; set; }
        public decimal? CurrentBalance { get; set; }
        public CustomerStatus? Status { get; set; }

        // Relations
        public int? BranchId { get; set; }
    }
}
