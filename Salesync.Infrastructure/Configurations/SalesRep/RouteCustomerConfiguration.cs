using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.SalesRep.Entities;

namespace Salesync.Infrastructure.Configurations.SalesRep
{
    public class RouteCustomerConfiguration : IEntityTypeConfiguration<RouteCustomer>
    {
        public void Configure(EntityTypeBuilder<RouteCustomer> builder)
        {
            builder.ToTable("RouteCustomers");
            builder.HasKey(rc=>rc.Id);

            builder.Property(rc => rc.VisitSequence)
                .IsRequired();
            builder.Property(rc => rc.VisitDays)
                .HasMaxLength(100);
            builder.Property(rc => rc.Notes)
                .HasMaxLength(500);


            // Relationships
            builder.HasOne(rc => rc.Route)
                .WithMany(r => r.RouteCustomers)
                .HasForeignKey(rc => rc.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(rc => rc.Customer)
                .WithMany()
                .HasForeignKey(rc => rc.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(rc => new { rc.RouteId, rc.CustomerId })
                .IsUnique();
        }
    }
}
