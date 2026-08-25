using Salesync.Domain.Common.Enums.SalesRep;

namespace Salesync.Application.Modules.SalesRep.Dtos.SalesRepDto
{
    public class SalesRepDto
    {
        public int Id { get; set; }

        public string SalesRepCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }

        public string? CategoryCode { get; set; }

        public SalesRepType? SalesRepType { get; set; }

        public int BranchId { get; set; }
        public string? BranchName { get; set; }

        public int? SupervisorId { get; set; }
        public string? SupervisorName { get; set; }

        public string? UserId { get; set; }

        public decimal? CreditLimit { get; set; }

        public int? OutOfRouteLimit { get; set; }
        public int? OutOfOrderLimit { get; set; }

        public bool AllowCreditOverride { get; set; }

        public bool ProofOfVisit { get; set; }
        public int? MaxVisitsWithoutProof { get; set; }

        public int? BusinessUnitId { get; set; }

        public string? ProfileImageUrl { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}