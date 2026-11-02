using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.Sales.Entities;
namespace Salesync.Infrastructure.Configurations.Sales;
public class InvoiceReturnReceiptConfiguration : IEntityTypeConfiguration<InvoiceReturnReceipt>
{
    public void Configure(EntityTypeBuilder<InvoiceReturnReceipt> b)
    {
        b.ToTable("InvoiceReturnReceipts", t => t.HasCheckConstraint("CK_ReturnReceipt_Amounts",
            "[TotalAmount] >= 0 AND [AppliedToInvoiceAmount] >= 0 AND [CustomerCreditAmount] >= 0 AND [TotalAmount] = [AppliedToInvoiceAmount] + [CustomerCreditAmount]"));
        b.HasKey(x => x.Id);
        b.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.ReceiptNumber).HasMaxLength(50).IsRequired();
        b.Property(x => x.CreditNoteNumber).HasMaxLength(50).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.CreatedBy).HasMaxLength(450);
        b.Property(x => x.TotalAmount).HasPrecision(18, 2);
        b.Property(x => x.AppliedToInvoiceAmount).HasPrecision(18, 2);
        b.Property(x => x.CustomerCreditAmount).HasPrecision(18, 2);
        b.HasIndex(x => x.OperationId).IsUnique();
        b.HasIndex(x => x.ReceiptNumber).IsUnique();
        b.HasIndex(x => x.CreditNoteNumber).IsUnique();
        b.HasIndex(x => new { x.CustomerId, x.CreatedAt });
        b.HasOne(x => x.InvoiceReturn).WithMany().HasForeignKey(x => x.InvoiceReturnId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Invoice).WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.SalesRep).WithMany().HasForeignKey(x => x.SalesRepId).OnDelete(DeleteBehavior.Restrict);
    }
}
public class InvoiceReturnReceiptItemConfiguration : IEntityTypeConfiguration<InvoiceReturnReceiptItem>
{
    public void Configure(EntityTypeBuilder<InvoiceReturnReceiptItem> b)
    {
        b.ToTable("InvoiceReturnReceiptItems", t => t.HasCheckConstraint("CK_ReturnReceiptItem_Quantity",
            "[Quantity] >= 0 AND [BonusQuantity] >= 0 AND ([Quantity] > 0 OR [BonusQuantity] > 0) AND [TotalAmount] >= 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.ProductName).HasMaxLength(250).IsRequired();
        b.Property(x => x.ItemCode).HasMaxLength(100).IsRequired();
        b.Property(x => x.TotalAmount).HasPrecision(18, 2);
        b.HasIndex(x => new { x.ReceiptId, x.InvoiceReturnItemId }).IsUnique();
        b.HasOne(x => x.Receipt).WithMany(x => x.Items).HasForeignKey(x => x.ReceiptId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.InvoiceReturnItem).WithMany().HasForeignKey(x => x.InvoiceReturnItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}
