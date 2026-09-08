using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.Treasury.Entities;

namespace Salesync.Infrastructure.Configurations.Treasury
{
    public class TreasuryTransactionConfiguration
        : IEntityTypeConfiguration<TreasuryTransaction>
    {
        public void Configure(
            EntityTypeBuilder<TreasuryTransaction> builder)
        {
            builder.ToTable("TreasuryTransactions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.BalanceBefore)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.BalanceAfter)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.TransactionDate)
                .IsRequired();

            builder.Property(x => x.ReferenceNumber)
                .HasMaxLength(100);

            builder.Property(x => x.Notes)
                .HasMaxLength(500);

            builder.Property(x => x.CreatedByUserId)
                .HasMaxLength(450);

            builder.HasOne(x => x.CashBox)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.CashBoxId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.CashReceipt)
                .WithMany()
                .HasForeignKey(x => x.CashReceiptId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ExpenseCategory)
                 .WithMany(x => x.TreasuryTransactions)
                 .HasForeignKey(x => x.ExpenseCategoryId)
                 .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.ExpenseCategoryId);
            builder.HasIndex(x => x.CashBoxId);

            builder.HasIndex(x => x.TransactionDate);

            builder.HasIndex(x => x.ReferenceNumber);

            builder.HasIndex(x => x.CashReceiptId);
        }
    }
}