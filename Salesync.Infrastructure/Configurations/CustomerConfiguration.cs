using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Entities;

namespace Salesync.Infrastructure.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("Customers");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Phone)
                .IsRequired()
                .HasMaxLength(15);

            builder.Property(c => c.Email)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Type)
                .IsRequired();

            builder.Property(c => c.CompanyName)
                .HasMaxLength(100);

            builder.Property(c => c.Address)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.Status)
                .IsRequired();

            builder.Property(c => c.CreditLimit)
                .HasPrecision(18, 2)
                .HasDefaultValue(0);

            builder.Property(c => c.CurrentBalance)
                .HasPrecision(18, 2)
                .HasDefaultValue(0);

            builder.Property(c => c.TotalPurchaseAmount)
                .HasPrecision(18, 2)
                .HasDefaultValue(0);

            builder.Property(c => c.CreatedAt)
                .IsRequired();

            builder.Property(c => c.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Indexes
            builder.HasIndex(c => c.Email).IsUnique();
            builder.HasIndex(c => c.Phone);
            builder.HasIndex(c => c.Name);
        }
    }
}
