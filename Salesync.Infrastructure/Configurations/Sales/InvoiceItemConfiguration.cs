using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.Sales.Entities;

namespace Salesync.Infrastructure.Configurations.Sales
{
    public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
    {
        public void Configure(EntityTypeBuilder<InvoiceItem> builder)
        {
            builder.ToTable("InvoiceItems");
            builder.HasKey(i => i.Id);

            builder.Property(i => i.ProductName)
                .IsRequired()
                .HasMaxLength(100);
            builder.Property(i => i.ItemCode)
                .IsRequired()
                .HasMaxLength(40);
            builder.Property(i => i.UnitPrice)
                .HasPrecision(18, 2);
            builder.Property(i => i.DiscountAmount)
                .HasPrecision(18, 2);
            builder.Property(i => i.DiscountPercentage)
                .HasPrecision(5, 2);
            builder.Property(i => i.TaxAmount)
                .HasPrecision(18, 2);
            builder.Property(i => i.NetAmount)
                .HasPrecision(18, 2);
            builder.Property(i => i.Notes)
                .HasMaxLength(500);

            builder.HasOne(i => i.Invoice)
                .WithMany(inv => inv.InvoiceItems)
                .HasForeignKey(i => i.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
