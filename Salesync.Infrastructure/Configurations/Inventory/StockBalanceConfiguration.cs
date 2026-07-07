using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.Inventory.Entities;

namespace Salesync.Infrastructure.Configurations.Inventory
{
    public class StockBalanceConfiguration : IEntityTypeConfiguration<StockBalance>
    {
        public void Configure(EntityTypeBuilder<StockBalance> builder)
        {
            builder.ToTable("StockBalances");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Quantity)
                .IsRequired();

            builder.Property(x => x.LastUpdatedAt)
                .IsRequired();

            builder.HasIndex(x => new { x.ProductId, x.WarehouseId })
                .IsUnique();

            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Warehouse)
                .WithMany()
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}