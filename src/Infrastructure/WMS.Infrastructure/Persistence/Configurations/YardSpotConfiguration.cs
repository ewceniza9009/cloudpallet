using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Aggregates.Warehouse;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Configurations;

public class YardSpotConfiguration : IEntityTypeConfiguration<YardSpot>
{
    public void Configure(EntityTypeBuilder<YardSpot> builder)
    {
        builder.ToTable("YardSpots");
        builder.HasKey(ys => ys.Id);

        builder.Property(ys => ys.SpotNumber)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasIndex(ys => new { ys.WarehouseId, ys.SpotNumber }).IsUnique();

        builder.Property(ys => ys.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne<Truck>()
            .WithMany()
            .HasForeignKey(ys => ys.CurrentTruckId)
            .OnDelete(DeleteBehavior.SetNull);          
    }
}