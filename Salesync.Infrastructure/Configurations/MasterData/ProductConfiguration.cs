using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.MasterData.Entities;

namespace Salesync.Infrastructure.Configurations.MasterData
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                .UseIdentityColumn();

            builder.HasIndex(p => p.ItemCode)
                .IsUnique();

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.Description)
                .HasMaxLength(500);

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

            builder.Property(p => p.CreatedAt)
                .IsRequired();

            builder.Property(p => p.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Relationships
            builder.HasOne(p => p.Warehouse)
                .WithMany(w => w.Products)
                .HasForeignKey(p => p.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(p => p.ItemCode).IsUnique();
            builder.HasIndex(p => p.SKU).IsUnique();
            builder.HasIndex(p => p.Barcode).IsUnique();
            builder.HasIndex(p => p.Name);
        }
    }
}
