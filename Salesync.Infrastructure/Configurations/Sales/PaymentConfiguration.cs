using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.Sales.Entities;

namespace Salesync.Infrastructure.Configurations.Sales
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payments");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.PaymentNumber)
                .IsRequired()
                .HasMaxLength(50);
            builder.Property(p => p.Amount)
                .HasPrecision(18, 2);
            builder.Property(p => p.CheckNumber)
                .HasMaxLength(50);
            builder.Property(p => p.TransactionReference)
                .HasMaxLength(100);
            builder.Property(p => p.BankName)
                .HasMaxLength(100);
            builder.Property(p => p.Notes)
                .HasMaxLength(500);

            builder.HasOne(p => p.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Customer)
                .WithMany()
                .HasForeignKey(p => p.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.SalesRep)
                .WithMany()
                .HasForeignKey(p => p.SalesRepId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasOne(p => p.SalesRepSession)
                .WithMany(s => s.Payments)
                .HasForeignKey(p => p.SalesRepSessionId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasIndex(p => p.PaymentNumber).IsUnique();
        }
    }
}
