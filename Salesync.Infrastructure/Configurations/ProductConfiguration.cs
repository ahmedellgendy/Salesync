using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Entities;

namespace Salesync.Infrastructure.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.Description)
                .HasMaxLength(500);

            builder.Property(p => p.UnitPrice)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(p => p.CostPrice)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(p => p.DiscountPercentage)
                .HasPrecision(18, 2);

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
            builder.HasIndex(p => p.SKU).IsUnique();
            builder.HasIndex(p => p.Barcode).IsUnique();
            builder.HasIndex(p => p.Name);
        }
    }
}
