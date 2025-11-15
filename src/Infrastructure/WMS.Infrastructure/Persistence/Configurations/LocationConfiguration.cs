using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Aggregates.Warehouse;

namespace WMS.Infrastructure.Persistence.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations");
        builder.HasKey(l => l.Id);

        builder.OwnsOne(l => l.CapacityWeight);

        builder.Property(l => l.Barcode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(l => l.Barcode).IsUnique();
    }
}