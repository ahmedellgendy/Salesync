using Salesync.Domain.Common;
using Salesync.Domain.Modules.MasterData.Entities;
using SalesRepEntity = Salesync.Domain.Modules.SalesRep.Entities.SalesRep;

namespace Salesync.Domain.Modules.LoadRequest.Entities
{
    public class SalesRepInventory : BaseEntity
    {
        public int SalesRepId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        public SalesRepEntity SalesRep { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
