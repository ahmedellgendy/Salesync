using Salesync.Domain.Enums;

namespace Salesync.Application.Dtos.CustomerDto
{
    public class CreateCustomerDto
    {
        // Basic Information (required)
        public required string Name { get; set; }
        public required string Phone { get; set; }
        public required string Email { get; set; }

        // Address Information (required)
        public required string Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }

        // Customer Type 
        public CustomerType Type { get; set; } = CustomerType.Individual;
        public string? CompanyName { get; set; }

        // Business Information 
        public decimal CreditLimit { get; set; } = 0;
        public CustomerStatus Status { get; set; } = CustomerStatus.Pending;

        // Relations 
        public int? BranchId { get; set; }
    }
}
