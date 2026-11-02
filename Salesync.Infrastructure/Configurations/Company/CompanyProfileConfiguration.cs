using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Salesync.Domain.Modules.Company.Entities;

namespace Salesync.Infrastructure.Configurations.Company;

public class CompanyProfileConfiguration
    : IEntityTypeConfiguration<CompanyProfile>
{
    public void Configure(
        EntityTypeBuilder<CompanyProfile> builder)
    {
        builder.ToTable("CompanyProfiles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ShortName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.LogoUrl)
            .HasMaxLength(500);

        builder.Property(x => x.Address)
            .HasMaxLength(500);

        builder.Property(x => x.Phone)
            .HasMaxLength(50);

        builder.Property(x => x.Email)
            .HasMaxLength(200);

        builder.Property(x => x.TaxRegistrationNumber)
            .HasMaxLength(100);

        builder.Property(x => x.CommercialRegistrationNumber)
            .HasMaxLength(100);

        builder.Property(x => x.Country)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.PrimaryColor)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.SecondaryColor)
            .IsRequired()
            .HasMaxLength(20);
    }
}