using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.MasterData.Entities;

namespace Salesync.Infrastructure.Configurations.MasterData
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("Customers");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                .UseIdentityColumn();
            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);
            builder.Property(c => c.Phone)
                .HasMaxLength(15);
            builder.Property(c => c.Email)
                .HasMaxLength(100);

            builder.Property(c => c.Country)
                .HasMaxLength(50);
            builder.Property(c => c.Address)
                .HasMaxLength(200);
            builder.Property(c => c.Area)
                .HasMaxLength(50);
            builder.Property(c => c.City)
                .HasMaxLength(50);
            builder.Property(c => c.District)
                .HasMaxLength(50);
            builder.Property(c => c.Region)
                .HasMaxLength(50);
            builder.Property(c => c.PostalCode)
                .HasMaxLength(20);

            builder.Property(c => c.Latitude)
                .HasPrecision(18, 8);
            builder.Property(c => c.Longitude)
                .HasPrecision(18, 8);

            builder.Property(c => c.CategoryCode)
                .HasMaxLength(20);
            builder.Property(c => c.SalesSectorCode)
                .HasMaxLength(20);
            builder.Property(c => c.ClassId)
                .HasMaxLength(20);
            builder.Property(c => c.Type)
                .IsRequired();

            builder.Property(c => c.CreditLimit)
                .HasPrecision(18, 2)
                .HasDefaultValue(0);
            builder.Property(c => c.CurrentBalance)
                .HasPrecision(18, 2)
                .HasDefaultValue(0);
            builder.Property(c => c.OrderCeiling)
               .HasPrecision(18, 2);
            builder.Property(c => c.TotalPurchaseAmount)
                .HasPrecision(18, 2)
                .HasDefaultValue(0);

            builder.Property(c => c.Status)
                .IsRequired();
            builder.Property(c => c.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
            builder.Property(c => c.IsActive)
                .HasDefaultValue(true);


            builder.HasOne(c => c.Branch)
               .WithMany()
               .HasForeignKey(c => c.BranchId)
               .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(c => c.Name);
            builder.HasIndex(c => c.Phone);
            builder.HasIndex(c => c.Email).IsUnique();
            builder.HasIndex(c => c.AccountNumber);
            builder.HasIndex(c => c.TaxId);

           
        }
    }
}
