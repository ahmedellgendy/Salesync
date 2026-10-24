using Salesync.Domain.Common;

namespace Salesync.Domain.Modules.Sales.Entities;

/// Durable receipt for a completed command, committed atomically with its effects.
/// Not a queue. Absence of a receipt never proves a command was not in flight.
public sealed class OperationExecution : BaseEntity
{
    public required string ActorId { get; set; }
    public required string OperationKey { get; set; }
    public required string RequestHash { get; set; }
    public required string Operation { get; set; }
    public int? SalesRepId { get; set; }
    public int? ResourceId { get; set; }
    public required string ResponseJson { get; set; }
    public int ResponseStatusCode { get; set; }
    public DateTime CompletedAt { get; set; }
}
