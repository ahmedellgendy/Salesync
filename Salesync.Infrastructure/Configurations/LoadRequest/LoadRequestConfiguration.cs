using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LoadRequestEntity = Salesync.Domain.Modules.LoadRequest.Entities.LoadRequest;

namespace Salesync.Infrastructure.Configurations.LoadRequest
{
    public class LoadRequestConfiguration : IEntityTypeConfiguration<LoadRequestEntity>
    {
        public void Configure(EntityTypeBuilder<LoadRequestEntity> builder)
        {
            builder.ToTable("LoadRequests");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.LoadRequestNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(x => x.LoadRequestNumber)
                .IsUnique();

            builder.Property(x => x.RequestDate)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.ApprovedByUserId)
                .HasMaxLength(450);

            builder.Property(x => x.RejectedByUserId)
                .HasMaxLength(450);

            builder.Property(x => x.ConfirmedByUserId)
                .HasMaxLength(450);

            builder.Property(x => x.CancelledByUserId)
                .HasMaxLength(450);

            builder.Property(x => x.RejectionReason)
                .HasMaxLength(500);

            builder.Property(x => x.Notes)
                .HasMaxLength(500);

            builder.HasOne(x => x.SalesRep)
                .WithMany()
                .HasForeignKey(x => x.SalesRepId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Warehouse)
                .WithMany()
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Items)
                .WithOne(x => x.LoadRequest)
                .HasForeignKey(x => x.LoadRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}