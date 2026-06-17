using Salesync.Domain.Common.Enums;

namespace Salesync.Application.Modules.SalesRep.Dtos.SalesRepDto
{
    public class UpdateSalesRepDto
    {
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public SalesRepType? SalesRepType { get; set; }
        public int? SupervisorId { get; set; }
        public decimal? CreditLimit { get; set; }
        public int? OutOfRouteLimit { get; set; }
        public int? OutOfOrderLimit { get; set; }
        public bool? AllowCreditOverride { get; set; }
        public bool? ProofOfVisit { get; set; }
        public int? MaxVisitsWithoutProof { get; set; }
        public bool? IsActive { get; set; }
        public int? BusinessUnitId { get; set; }
    }
}
