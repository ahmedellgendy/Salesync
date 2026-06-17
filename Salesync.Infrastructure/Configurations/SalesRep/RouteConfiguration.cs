using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.SalesRep.Entities;

namespace Salesync.Infrastructure.Configurations.SalesRep
{
    public class RouteConfiguration : IEntityTypeConfiguration<Route>
    {
        public void Configure(EntityTypeBuilder<Route> builder)
        {
            builder.ToTable("Routes");
            builder.HasKey(r => r.Id);

            builder.Property(r => r.RouteCode)
                .IsRequired()
                .HasMaxLength(15);
            builder.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(100);


            // Relationships
            builder.HasOne(r => r.Branch)
                .WithMany()
                .HasForeignKey(r => r.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(r => r.RouteCustomers)
                .WithOne(rc => rc.Route)
                .HasForeignKey(rc => rc.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(r => r.RouteCode)
                .IsUnique();
        }
    }
}
