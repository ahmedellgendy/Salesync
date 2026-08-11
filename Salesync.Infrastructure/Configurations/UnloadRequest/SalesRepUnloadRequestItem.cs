using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.UnloadRequest.Entities;

namespace Salesync.Infrastructure.Configurations.UnloadRequest
{
    public class SalesRepUnloadRequestItemConfiguration : IEntityTypeConfiguration<SalesRepUnloadRequestItem>
    {
        public void Configure(EntityTypeBuilder<SalesRepUnloadRequestItem> builder)
        {
            builder.ToTable("SalesRepUnloadRequestItems");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ProductName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.ItemCode)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.SmallUnit)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.LargeUnit)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.UnitsPerLargeUnit)
                .IsRequired();

            builder.Property(x => x.RequestedLargeQuantity)
                .IsRequired();

            builder.Property(x => x.RequestedSmallQuantity)
                .IsRequired();

            builder.Property(x => x.ConfirmedSmallQuantity)
                .IsRequired();

            builder.Property(x => x.RequestedQuantity)
                .IsRequired();

            builder.Property(x => x.ConfirmedLargeQuantity)
                .IsRequired();

            builder.Property(x => x.ConfirmedQuantity)
                .IsRequired();

            builder.Property(x => x.VarianceQuantity)
                .IsRequired();

            builder.Property(x => x.SalesRepInventoryBeforeUnload)
                .IsRequired();

            builder.Property(x => x.SalesRepInventoryAfterUnload)
                .IsRequired();

            builder.Property(x => x.SalesRepNotes)
                .HasMaxLength(500);

            builder.Property(x => x.WarehouseNotes)
                .HasMaxLength(500);

            builder.HasIndex(x => x.SalesRepUnloadRequestId);
            builder.HasIndex(x => x.ProductId);
        }
    }
}