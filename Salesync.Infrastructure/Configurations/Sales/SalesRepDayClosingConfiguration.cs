using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.Sales.Entities;

namespace Salesync.Infrastructure.Configurations.Sales
{
    public class SalesRepDayClosingConfiguration : IEntityTypeConfiguration<SalesRepDayClosing>
    {
        public void Configure(EntityTypeBuilder<SalesRepDayClosing> builder)
        {
            builder.ToTable("SalesRepDayClosings");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ClosingNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.TotalSalesAmount)
                .HasPrecision(18, 2);

            builder.Property(x => x.TotalCollectionAmount)
                .HasPrecision(18, 2);

            builder.Property(x => x.TotalReturnAmount)
                .HasPrecision(18, 2);

            builder.Property(x => x.ExpectedCashAmount)
                .HasPrecision(18, 2);

            builder.Property(x => x.ActualCashAmount)
                .HasPrecision(18, 2);

            builder.Property(x => x.CashVariance)
                .HasPrecision(18, 2);

            builder.Property(x => x.SubmittedByUserId)
                .HasMaxLength(450);

            builder.Property(x => x.ApprovedByUserId)
                .HasMaxLength(450);

            builder.Property(x => x.RejectedByUserId)
                .HasMaxLength(450);

            builder.Property(x => x.CancelledByUserId)
                .HasMaxLength(450);

            builder.Property(x => x.RejectionReason)
                .HasMaxLength(500);

            builder.Property(x => x.Notes)
                .HasMaxLength(500);

            builder.HasIndex(x => x.ClosingNumber)
                .IsUnique();

            builder.HasIndex(x => x.SalesRepId);
            builder.HasIndex(x => x.SalesRepSessionId)
                .IsUnique()
                .HasFilter("[IsActive] = 1 AND [Status] IN (1, 2)");

            builder.HasIndex(x => x.WarehouseId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.ClosingDate);

            builder.HasOne(x => x.SalesRep)
                .WithMany()
                .HasForeignKey(x => x.SalesRepId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.SalesRepSession)
                .WithMany()
                .HasForeignKey(x => x.SalesRepSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Warehouse)
                .WithMany()
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Items)
                .WithOne(x => x.SalesRepDayClosing)
                .HasForeignKey(x => x.SalesRepDayClosingId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
