using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.Sales.Entities;

namespace Salesync.Infrastructure.Configurations.Sales
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.ToTable("Invoices");
            builder.HasKey(i => i.Id);

            builder.Property(i => i.InvoiceNumber)
                .IsRequired()
                .HasMaxLength(50);
            builder.HasIndex(i => i.InvoiceNumber)
                .IsUnique();
            builder.Property(i => i.SalesChannel)
                .HasConversion<int>();
            builder.Property(i => i.PaymentStatus)
                .HasConversion<int>();
            builder.Property(i => i.Status)
                .HasConversion<int>();
            builder.Property(i => i.SubTotal)
                .HasPrecision(18, 2);
            builder.Property(i => i.DiscountAmount)
                .HasPrecision(18, 2);
            builder.Property(i => i.TaxAmount)
                .HasPrecision(18, 2);
            builder.Property(i => i.TotalAmount)
                .HasPrecision(18, 2);
            builder.Property(i => i.PaidAmount)
                .HasPrecision(18, 2);
            builder.Property(i => i.Notes)
                .HasMaxLength(500);

            builder.HasOne(i => i.Customer)
                .WithMany(/*i => i.Invoices*/)
                .HasForeignKey(i => i.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(i => i.Warehouse)
                .WithMany(/*i => i.Invoices*/)
                .HasForeignKey(i => i.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(i => i.SalesRep)
                .WithMany(/*i => i.Invoices*/)
                .HasForeignKey(i => i.SalesRepId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(i => i.SalesRepSession)
                .WithMany(i => i.Invoices)
                .HasForeignKey(i => i.SalesRepSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(i => i.InvoiceItems)
                .WithOne(i => i.Invoice)
                .HasForeignKey(i => i.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(i => i.Payments)
                .WithOne(i => i.Invoice)
                .HasForeignKey(i => i.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(i => i.InvoiceReturns)
                .WithOne(i => i.Invoice)
                .HasForeignKey(i => i.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(i => i.InvoiceNumber).IsUnique();

        }
    }
}
