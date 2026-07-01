using Salesync.Domain.Common.Enums.SalesRep;

namespace Salesync.Application.Modules.SalesRep.Dtos.SalesRepDto
{
    public class SalesRepDto
    {
        public int Id { get; set; }
        public string SalesRepCode { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public SalesRepType? SalesRepType { get; set; }
        public int BranchId { get; set; }
        public int? SupervisorId { get; set; }
        public decimal? CreditLimit { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
