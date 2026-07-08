using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LoadRequestItemEntity = Salesync.Domain.Modules.LoadRequest.Entities.LoadRequestItem;


namespace Salesync.Infrastructure.Configurations.LoadRequest
{
    public class LoadRequestItemConfiguration : IEntityTypeConfiguration<LoadRequestItemEntity>
    {
        public void Configure(EntityTypeBuilder<LoadRequestItemEntity> builder)
        {
            builder.ToTable("LoadRequestItems");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ProductName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.ItemCode)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.RequestedQuantity)
                .IsRequired();

            builder.Property(x => x.ApprovedQuantity)
                .IsRequired();

            builder.Property(x => x.ConfirmedQuantity)
                .IsRequired();

            builder.Property(x => x.Notes)
                .HasMaxLength(500);

            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.LoadRequestId, x.ProductId })
                .IsUnique();
        }
    }
}