using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.Sales.Entities;

namespace Salesync.Infrastructure.Configurations.Sales;

public sealed class OperationExecutionConfiguration : IEntityTypeConfiguration<OperationExecution>
{
    public void Configure(EntityTypeBuilder<OperationExecution> builder)
    {
        builder.ToTable("OperationExecutions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActorId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.OperationKey).HasMaxLength(36).IsRequired();
        builder.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Operation).HasMaxLength(240).IsRequired();
        builder.Property(x => x.ResponseJson).IsRequired();
        builder.HasIndex(x => new { x.ActorId, x.OperationKey }).IsUnique();
        builder.HasIndex(x => new { x.SalesRepId, x.CompletedAt });
    }
}
