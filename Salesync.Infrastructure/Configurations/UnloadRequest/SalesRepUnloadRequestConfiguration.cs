using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.UnloadRequest.Entities;

namespace Salesync.Infrastructure.Configurations.UnloadRequest
{
    public class SalesRepUnloadRequestConfiguration : IEntityTypeConfiguration<SalesRepUnloadRequest>
    {
        public void Configure(EntityTypeBuilder<SalesRepUnloadRequest> builder)
        {
            builder.ToTable("SalesRepUnloadRequests");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.RequestNumber)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.RequestedAt)
                .IsRequired();

            builder.Property(x => x.RequestedByUserId)
                .HasMaxLength(450);

            builder.Property(x => x.ConfirmedByUserId)
                .HasMaxLength(450);

            builder.Property(x => x.CancelledByUserId)
                .HasMaxLength(450);

            builder.Property(x => x.TotalRequestedQuantity)
                .IsRequired();

            builder.Property(x => x.TotalConfirmedQuantity)
                .IsRequired();

            builder.Property(x => x.TotalVarianceQuantity)
                .IsRequired();

            builder.Property(x => x.TotalItems)
                .IsRequired();

            builder.Property(x => x.SalesRepNotes)
                .HasMaxLength(500);

            builder.Property(x => x.WarehouseNotes)
                .HasMaxLength(500);

            builder.Property(x => x.CancellationReason)
                .HasMaxLength(500);

            builder.HasMany(x => x.Items)
                .WithOne(x => x.SalesRepUnloadRequest)
                .HasForeignKey(x => x.SalesRepUnloadRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.RequestNumber)
                .IsUnique();

            builder.HasIndex(x => x.SalesRepId);
            builder.HasIndex(x => x.SalesRepSessionId);
            builder.HasIndex(x => x.WarehouseId);
            builder.HasIndex(x => x.BranchId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.RequestedAt);
        }
    }
}