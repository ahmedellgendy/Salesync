using Salesync.Domain.Common;

namespace Salesync.Domain.Modules.SalesRep.Entities
{
    public class SalesRepRoute : BaseEntity
    {
        // Composite Key
        public int SalesRepId { get; set; }
        public int RouteId { get; set; }
        public DateTime AssignedDate { get; set; }
        public DateTime? UnassignedDate { get; set; }
        public bool IsCurrent { get; set; } = true;
        public string? Notes { get; set; }


        // Navigation Properties
        public SalesRep SalesRep { get; set; } = null!;
        public Route Route { get; set; } = null!;
    }
}
