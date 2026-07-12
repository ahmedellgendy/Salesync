using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.Sales.Entities;

namespace Salesync.Infrastructure.Configurations.Sales
{
    public class SalesRepDayClosingItemConfiguration : IEntityTypeConfiguration<SalesRepDayClosingItem>
    {
        public void Configure(EntityTypeBuilder<SalesRepDayClosingItem> builder)
        {
            builder.ToTable("SalesRepDayClosingItems");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Notes)
                .HasMaxLength(500);

            builder.HasIndex(x => x.SalesRepDayClosingId);
            builder.HasIndex(x => x.ProductId);

            builder.HasIndex(x => new { x.SalesRepDayClosingId, x.ProductId })
                .IsUnique();

            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}