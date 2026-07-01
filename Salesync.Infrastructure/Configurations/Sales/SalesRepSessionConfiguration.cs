using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.Sales.Entities;

namespace Salesync.Infrastructure.Configurations.Sales
{
    public class SalesRepSessionConfiguration : IEntityTypeConfiguration<SalesRepSession>
    {
        public void Configure(EntityTypeBuilder<SalesRepSession> builder)
        {
            builder.ToTable("SalesRepSessions");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.GrossSales)
                .HasPrecision(18, 2);
            builder.Property(s => s.NetSales)
                .HasPrecision(18, 2);
            builder.Property(s => s.TotalCollection)
                .HasPrecision(18, 2);
            builder.Property(s => s.TotalReturnAmount)
                .HasPrecision(18, 2);
            builder.Property(x => x.Notes)
                .HasMaxLength(500);

            builder.HasOne(s => s.SalesRep)
                .WithMany()
                .HasForeignKey(s => s.SalesRepId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => new { s.SalesRepId, s.WorkingDate }).IsUnique();
        }
    }
}
