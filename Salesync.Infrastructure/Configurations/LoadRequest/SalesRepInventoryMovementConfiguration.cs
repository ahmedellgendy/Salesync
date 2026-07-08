using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.LoadRequest.Entities;

namespace Salesync.Infrastructure.Configurations.LoadRequest
{
    public class SalesRepInventoryMovementConfiguration : IEntityTypeConfiguration<SalesRepInventoryMovement>
    {
        public void Configure(EntityTypeBuilder<SalesRepInventoryMovement> builder)
        {
            builder.ToTable("SalesRepInventoryMovements");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Quantity)
                .IsRequired();

            builder.Property(x => x.MovementType)
                .IsRequired();

            builder.Property(x => x.Source)
                .IsRequired();

            builder.Property(x => x.SourceNumber)
                .HasMaxLength(100);

            builder.Property(x => x.MovementDate)
                .IsRequired();

            builder.Property(x => x.Notes)
                .HasMaxLength(500);

            builder.HasIndex(x => x.SalesRepId);
            builder.HasIndex(x => x.ProductId);
            builder.HasIndex(x => x.SourceId);
            builder.HasIndex(x => x.MovementDate);

            builder.HasOne(x => x.SalesRep)
                .WithMany()
                .HasForeignKey(x => x.SalesRepId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}