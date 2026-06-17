using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Salesync.Infrastructure.Configurations.SalesRep
{
    public class SalesRepConfiguration : IEntityTypeConfiguration<Domain.Modules.SalesRep.Entities.SalesRep>
    {
        public void Configure(EntityTypeBuilder<Domain.Modules.SalesRep.Entities.SalesRep> builder)
        {
            builder.ToTable("SalesReps");
            builder.HasKey(sr => sr.Id);

            builder.Property(sr => sr.SalesRepCode)
                .IsRequired()
                .HasMaxLength(15);
            builder.Property(sr => sr.Name)
                .IsRequired()
                .HasMaxLength(100);
            builder.Property(sr => sr.Phone)
                .IsRequired()
                .HasMaxLength(15);

            builder.Property(s => s.Mobile)
                .HasMaxLength(15);
            builder.Property(s => s.Email)
                .HasMaxLength(100);
            builder.Property(s => s.Address)
                .HasMaxLength(200);
            builder.Property(s => s.CategoryCode)
                .HasMaxLength(15);
            builder.Property(s => s.UserId)
                .HasMaxLength(450);
            builder.Property(s => s.CreditLimit)
                .HasPrecision(18, 2);

            // Relationships
            builder.HasOne(s => s.Branch)
                .WithMany()
                .HasForeignKey(s => s.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.Supervisor)
                .WithMany(s => s.TeamMembers)
                .HasForeignKey(s => s.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(s => s.SalesRepCode)
                .IsUnique();


        }
    }
}
