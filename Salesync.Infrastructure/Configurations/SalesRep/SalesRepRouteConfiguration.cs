using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.SalesRep.Entities;

namespace Salesync.Infrastructure.Configurations.SalesRep
{
    public class SalesRepRouteConfiguration : IEntityTypeConfiguration<SalesRepRoute>
    {
        public void Configure(EntityTypeBuilder<SalesRepRoute> builder)
        {
            builder.ToTable("SalesRepRoutes");

            // Composite Key
            builder.HasKey(sr => new { sr.SalesRepId, sr.RouteId });

            builder.Property(sr => sr.Notes)
                .HasMaxLength(500);

            // Relationships
            builder.HasOne(sr => sr.SalesRep)
                .WithMany(s => s.SalesRepRoutes)
                .HasForeignKey(sr => sr.SalesRepId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(sr => sr.Route)
                .WithMany(r => r.SalesRepRoutes)
                .HasForeignKey(sr => sr.RouteId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
