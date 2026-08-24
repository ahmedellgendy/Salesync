using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.MasterData.Entities;

namespace Salesync.Infrastructure.Configurations.MasterData
{
    public class ProductConfiguration :
        IEntityTypeConfiguration<Product>
    {
        public void Configure(
            EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                .UseIdentityColumn();

            builder.Property(p => p.ItemCode)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.Description)
                .HasMaxLength(500);

            builder.Property(p => p.SKU)
                .HasMaxLength(50);

            builder.Property(p => p.Barcode)
                .HasMaxLength(50);

            builder.Property(p => p.Unit)
                .HasMaxLength(20);

            builder.Property(p => p.UnitPrice)
                .HasPrecision(18, 2)
                .HasDefaultValue(0);

            builder.Property(p => p.CostPrice)
                .HasPrecision(18, 2)
                .HasDefaultValue(0);

            builder.Property(p => p.DiscountPercentage)
                .HasPrecision(18, 2);

            builder.Property(p => p.MinStockLevel)
                .HasDefaultValue(0);

            builder.Property(p => p.MaxStockLevel)
                .HasDefaultValue(0);

            builder.Property(p => p.SmallUnit)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("قطعة");

            builder.Property(p => p.LargeUnit)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("كرتونة");

            builder.Property(p => p.UnitsPerLargeUnit)
                .IsRequired()
                .HasDefaultValue(1);

            builder.Property(p => p.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(p => p.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.HasOne(p => p.Warehouse)
                .WithMany(w => w.Products)
                .HasForeignKey(p => p.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasIndex(p => p.ItemCode)
                .IsUnique();

            builder.HasIndex(p => p.SKU)
                .IsUnique()
                .HasFilter("[SKU] IS NOT NULL");

            builder.HasIndex(p => p.Barcode)
                .IsUnique()
                .HasFilter("[Barcode] IS NOT NULL");

            builder.HasIndex(p => p.Name);
        }
    }
}