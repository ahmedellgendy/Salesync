using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.Treasury.Entities;

namespace Salesync.Infrastructure.Configurations.Treasury
{
    public class CashBoxConfiguration : IEntityTypeConfiguration<CashBox>
    {
        public void Configure(EntityTypeBuilder<CashBox> builder)
        {
            builder.ToTable("CashBoxes");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(x => x.CurrentBalance)
                .HasPrecision(18, 2);

            builder.Property(x => x.Notes)
                .HasMaxLength(500);

            builder.HasIndex(x => x.Code)
                .IsUnique();

            builder.HasOne(x => x.Branch)
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}