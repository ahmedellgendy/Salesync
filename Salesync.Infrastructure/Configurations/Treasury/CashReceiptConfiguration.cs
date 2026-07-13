using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.Treasury.Entities;

namespace Salesync.Infrastructure.Configurations.Treasury
{
    public class CashReceiptConfiguration : IEntityTypeConfiguration<CashReceipt>
    {
        public void Configure(EntityTypeBuilder<CashReceipt> builder)
        {
            builder.ToTable("CashReceipts");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ReceiptNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.ExpectedAmount)
                .HasPrecision(18, 2);

            builder.Property(x => x.ReceivedAmount)
                .HasPrecision(18, 2);

            builder.Property(x => x.VarianceAmount)
                .HasPrecision(18, 2);

            builder.Property(x => x.ReceivedByUserId)
                .HasMaxLength(450);

            builder.Property(x => x.Notes)
                .HasMaxLength(500);

            builder.HasIndex(x => x.ReceiptNumber)
                .IsUnique();

            builder.HasIndex(x => x.SalesRepDayClosingId)
                .IsUnique();

            builder.HasOne(x => x.CashBox)
                .WithMany(x => x.CashReceipts)
                .HasForeignKey(x => x.CashBoxId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.SalesRep)
                .WithMany()
                .HasForeignKey(x => x.SalesRepId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.SalesRepSession)
                .WithMany()
                .HasForeignKey(x => x.SalesRepSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.SalesRepDayClosing)
                .WithMany()
                .HasForeignKey(x => x.SalesRepDayClosingId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}