using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.Treasury.Entities;

namespace Salesync.Infrastructure.Configurations.Treasury
{
    public class SalesRepCashLedgerConfiguration : IEntityTypeConfiguration<SalesRepCashLedger>
    {
        public void Configure(EntityTypeBuilder<SalesRepCashLedger> builder)
        {
            builder.ToTable("SalesRepCashLedgers");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Amount)
                .HasPrecision(18, 2);

            builder.Property(x => x.BalanceAfter)
                .HasPrecision(18, 2);

            builder.Property(x => x.Source)
                .IsRequired();

            builder.Property(x => x.ReferenceNumber)
                .HasMaxLength(50);

            builder.Property(x => x.Notes)
                .HasMaxLength(500);

            builder.HasIndex(x => x.SalesRepId);

            builder.HasIndex(x => x.CashReceiptId);

            builder.HasOne(x => x.SalesRep)
                .WithMany()
                .HasForeignKey(x => x.SalesRepId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.CashReceipt)
                .WithMany()
                .HasForeignKey(x => x.CashReceiptId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}