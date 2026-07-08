using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.LoadRequest.Entities;

namespace Salesync.Infrastructure.Configurations.LoadRequest
{
    public class SalesRepInventoryConfiguration : IEntityTypeConfiguration<SalesRepInventory>
    {
        public void Configure(EntityTypeBuilder<SalesRepInventory> builder)
        {
            builder.ToTable("SalesRepInventories");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Quantity)
                .IsRequired();

            builder.Property(x => x.LastUpdatedAt)
                .IsRequired();

            builder.HasIndex(x => new { x.SalesRepId, x.ProductId })
                .IsUnique();

            builder.HasOne(x => x.SalesRep)
                .WithMany()
                .HasForeignKey(x => x.SalesRepId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}