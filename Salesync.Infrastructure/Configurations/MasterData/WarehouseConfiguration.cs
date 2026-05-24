using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.MasterData.Entities;

namespace Salesync.Infrastructure.Configurations.MasterData
{
    public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
    {
        public void Configure(EntityTypeBuilder<Warehouse> builder)
        {
            builder.ToTable("Warehouses");
            builder.HasKey(w => w.Id);

            builder.Property(w => w.Id)
                .UseIdentityColumn();

            builder.Property(w => w.WarehouseCode)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(w => w.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(w => w.Location)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(w => w.Latitude)
                .HasPrecision(18, 10);

            builder.Property(w => w.Longitude)
                .HasPrecision(18, 10);

            builder.Property(w => w.ErrorRadius)
                .HasPrecision(18, 2);

            builder.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(w => w.IsActive)
                .HasDefaultValue(true);

            // Relationships
            builder.HasOne(w => w.Branch)
                .WithMany(b => b.Warehouses)
                .HasForeignKey(w => w.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(w => w.Products)
                .WithOne(p => p.Warehouse)
                .HasForeignKey(p => p.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(w => w.WarehouseCode).IsUnique();
            builder.HasIndex(w => new { w.BranchId, w.Type });

        }
    }
}
