using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.MasterData.Entities;

namespace Salesync.Infrastructure.Configurations.MasterData
{
    public class BranchConfiguration : IEntityTypeConfiguration<Branch>
    {
        public void Configure(EntityTypeBuilder<Branch> builder)
        {
            builder.ToTable("Branches");
            builder.HasKey(b => b.Id);

            builder.Property(b => b.Id)
                .UseIdentityColumn();

            builder.Property(b => b.BranchCode)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(b => b.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(b => b.City)
                .HasMaxLength(100);

            builder.Property(b => b.Address)
                .HasMaxLength(200);

            builder.Property(b => b.Phone)
                .HasMaxLength(20);

            builder.Property(b => b.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(b => b.IsActive)
                .HasDefaultValue(true);

            // Relationships
            builder.HasMany(b => b.Warehouses)
                .WithOne(w => w.Branch)
                .HasForeignKey(w => w.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(b => b.BranchCode).IsUnique();
            builder.HasIndex(b => b.Name);
            builder.HasIndex(b => b.Phone);
        }
    }
}
