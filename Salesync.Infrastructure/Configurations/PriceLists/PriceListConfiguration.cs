using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.MasterData.Entities;

namespace Salesync.Infrastructure.Configurations.PriceLists
{
    public class PriceListConfiguration: IEntityTypeConfiguration<PriceList>
    {

        public void Configure(EntityTypeBuilder<PriceList> builder)
        {
            builder.ToTable("PriceLists");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Description)
                .HasMaxLength(500);

            builder.HasIndex(x => x.Code)
                .IsUnique();

            builder.HasIndex(x => x.IsDefault);

            builder.HasIndex(x => x.IsActive);
        }
    }
}
