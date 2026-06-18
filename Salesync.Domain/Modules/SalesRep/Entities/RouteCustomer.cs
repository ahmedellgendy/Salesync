using Salesync.Domain.Common;
using Salesync.Domain.Modules.MasterData.Entities;

namespace Salesync.Domain.Modules.SalesRep.Entities
{
    public class RouteCustomer : BaseEntity
    {
        public int RouteId { get; set; }
        public int CustomerId { get; set; }
        public int VisitSequence { get; set; } = 0;
        public string? VisitDays { get; set; }
        public string? Notes { get; set; }


        // Navigation Properties
        public Route Route { get; set; } = null!;
        public Customer Customer { get; set; } = null!;
    }
}
