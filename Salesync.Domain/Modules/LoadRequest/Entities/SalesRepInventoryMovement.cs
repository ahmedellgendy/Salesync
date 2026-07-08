using Salesync.Domain.Common;
using Salesync.Domain.Common.Enums.LoadRequest;
using Salesync.Domain.Modules.MasterData.Entities;
using SalesRepEntity = Salesync.Domain.Modules.SalesRep.Entities.SalesRep;

namespace Salesync.Domain.Modules.LoadRequest.Entities
{
    public class SalesRepInventoryMovement : BaseEntity
    {
        public int SalesRepId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public SalesRepInventoryMovementType MovementType { get; set; }
        public SalesRepInventoryMovementSource Source { get; set; }
        public int? SourceId { get; set; }
        public string? SourceNumber { get; set; }
        public DateTime MovementDate { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }

        public SalesRepEntity SalesRep { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
