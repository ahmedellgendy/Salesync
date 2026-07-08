using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CustomerVisitEntity = Salesync.Domain.Modules.CustomerVisit.Entities.CustomerVisit;


namespace Salesync.Infrastructure.Configurations.CustomerVisit
{
    public class CustomerVisitConfiguration : IEntityTypeConfiguration<CustomerVisitEntity>
    {
        public void Configure(EntityTypeBuilder<CustomerVisitEntity> builder)
        {
            builder.ToTable("CustomerVisits");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.VisitDate)
                .IsRequired();

            builder.Property(x => x.VisitType)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.Latitude)
                .HasPrecision(18, 6);

            builder.Property(x => x.Longitude)
                .HasPrecision(18, 6);

            builder.Property(x => x.Notes)
                .HasMaxLength(500);

            builder.HasIndex(x => x.SalesRepId);
            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.RouteId);
            builder.HasIndex(x => x.SalesRepSessionId);
            builder.HasIndex(x => x.VisitDate);
            builder.HasIndex(x => x.VisitType);
            builder.HasIndex(x => x.Status);

            builder.HasOne(x => x.SalesRep)
                .WithMany()
                .HasForeignKey(x => x.SalesRepId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Route)
                .WithMany()
                .HasForeignKey(x => x.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.SalesRepSession)
                .WithMany()
                .HasForeignKey(x => x.SalesRepSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Invoice)
                .WithMany()
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Payment)
                .WithMany()
                .HasForeignKey(x => x.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.InvoiceReturn)
                .WithMany()
                .HasForeignKey(x => x.InvoiceReturnId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}