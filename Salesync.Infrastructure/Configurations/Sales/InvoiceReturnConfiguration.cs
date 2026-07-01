using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.Sales.Entities;

namespace Salesync.Infrastructure.Configurations.Sales
{
    public class InvoiceReturnConfiguration : IEntityTypeConfiguration<InvoiceReturn>
    {
        public void Configure(EntityTypeBuilder<InvoiceReturn> builder)
        {
            builder.ToTable("InvoiceReturns");
            builder.HasKey(r => r.Id);

            builder.Property(r => r.ReturnNumber)
                .IsRequired()
                .HasMaxLength(50);
            builder.Property(r=>r.TotalAmount)
                .HasPrecision(18, 2);
            builder.Property(r => r.ReasonNotes)
                .HasMaxLength(500);

            builder.HasOne(r => r.Invoice)
               .WithMany(i => i.InvoiceReturns)
               .HasForeignKey(r => r.InvoiceId)
               .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Customer)
                .WithMany()
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.SalesRep)
                .WithMany()
                .HasForeignKey(r => r.SalesRepId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasIndex(r => r.ReturnNumber).IsUnique();
        }
    }
}
