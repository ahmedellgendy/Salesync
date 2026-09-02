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

            // Properties
            builder.Property(r => r.RouteCode)
                .IsRequired()
                .HasMaxLength(15);

            builder.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(r => r.Type)
                .HasMaxLength(50);

            builder.Property(r => r.RegionCode)
                .HasMaxLength(50);

            builder.Property(r => r.DistrictCode)
                .HasMaxLength(50);

            builder.Property(r => r.CityCode)
                .HasMaxLength(50);

            builder.Property(r => r.AreaCode)
                .HasMaxLength(50);

            builder.Property(r => r.RouteChannel)
                .HasMaxLength(50);

            builder.Property(r => r.RouteGTM)
                .HasMaxLength(50);

            builder.Property(r => r.RouteCategory)
                .HasMaxLength(50);

            // Relationships
            builder.HasOne(r => r.Branch)
                .WithMany()
                .HasForeignKey(r => r.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.AssignedSalesRep)
                .WithMany()
                .HasForeignKey(r => r.AssignedSalesRepId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasMany(r => r.RouteCustomers)
                .WithOne(rc => rc.Route)
                .HasForeignKey(rc => rc.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(r => r.RouteCode)
                .IsUnique();

            builder.HasIndex(r => r.BranchId);

            builder.HasIndex(r => r.AssignedSalesRepId);
        }
    }
}