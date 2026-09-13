using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.MasterData.Entities;

namespace Salesync.Infrastructure.Configurations.PriceLists;
public class ProductPriceConfiguration
    : IEntityTypeConfiguration<ProductPrice>
{
    public void Configure(EntityTypeBuilder<ProductPrice> builder)
    {
        builder.ToTable("ProductPrices");

        builder.HasKey(x => x.Id);


        builder.Property(x => x.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();


        builder.Property(x => x.DiscountPercentage)
            .HasPrecision(5, 2)
            .HasDefaultValue(0);


        builder.HasIndex(x => new
        {
            x.PriceListId,
            x.ProductId
        })
        .IsUnique();


        builder.HasOne(x => x.PriceList)
            .WithMany(x => x.ProductPrices)
            .HasForeignKey(x => x.PriceListId)
            .OnDelete(DeleteBehavior.Restrict);


        builder.HasOne(x => x.Product)
            .WithMany(x => x.Prices)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }

}
