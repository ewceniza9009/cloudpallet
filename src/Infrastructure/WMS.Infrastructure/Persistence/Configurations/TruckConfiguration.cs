using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Configurations;

public class TruckConfiguration : IEntityTypeConfiguration<Truck>
{
    public void Configure(EntityTypeBuilder<Truck> builder)
    {
        builder.ToTable("Trucks");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.LicensePlate)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(t => t.LicensePlate).IsUnique();

        builder.HasOne(t => t.Carrier)
            .WithMany(c => c.Trucks)
            .HasForeignKey(t => t.CarrierId);
    }
}