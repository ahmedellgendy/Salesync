using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.Sales.Entities;

namespace Salesync.Infrastructure.Configurations.Sales
{
    public class InvoiceReturnItemConfiguration : IEntityTypeConfiguration<InvoiceReturnItem>
    {
        public void Configure(EntityTypeBuilder<InvoiceReturnItem> builder)
        {
            builder.ToTable("InvoiceReturnItems");
            builder.HasKey(r => r.Id);

            builder.Property(r => r.ProductName)
                .IsRequired()
                .HasMaxLength(100);
            builder.Property(r => r.ItemCode)
                .IsRequired()
                .HasMaxLength(40);
            builder.Property(r => r.UnitPrice)
                .HasPrecision(18, 2);
            builder.Property(r => r.TotalAmount)
                .HasPrecision(18, 2);
            builder.Property(r => r.Notes)
                .HasMaxLength(500);

            builder.HasOne(r => r.InvoiceReturn)
                .WithMany(ir => ir.Items)
                .HasForeignKey(r => r.InvoiceReturnId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.Product)
                .WithMany()
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
