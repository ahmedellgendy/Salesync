using Salesync.Domain.Common.Enums;

namespace Salesync.Application.Modules.MasterData.Dtos.CustomerDto
{
    public class CustomerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }

        public CustomerType Type { get; set; }
        public string? CompanyName { get; set; }
        public CustomerStatus Status { get; set; }
        public decimal CurrentBalance { get; set; }


        public int? BranchId { get; set; }
        public string? BranchName { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
