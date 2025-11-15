using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Configurations;

public class CarrierConfiguration : IEntityTypeConfiguration<Carrier>
{
    public void Configure(EntityTypeBuilder<Carrier> builder)
    {
        builder.ToTable("Carriers");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);

        builder.Property(c => c.ScacCode)
            .IsRequired()
            .HasMaxLength(4); // SCAC is typically 4 letters
        builder.HasIndex(c => c.ScacCode).IsUnique();

        builder.Property(c => c.DotNumber).HasMaxLength(20);
        builder.Property(c => c.ContactName).HasMaxLength(100);
        builder.Property(c => c.ContactPhone).HasMaxLength(20);
        builder.Property(c => c.ContactEmail).HasMaxLength(100);
        builder.Property(c => c.InsurancePolicyNumber).HasMaxLength(50);

        builder.OwnsOne(c => c.Address);

        builder.HasMany(c => c.Trucks)
            .WithOne(t => t.Carrier)
            .HasForeignKey(t => t.CarrierId);
    }
}