using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Aggregates.Warehouse;

namespace WMS.Infrastructure.Persistence.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");
        builder.HasKey(r => r.Id);

        builder.OwnsOne(r => r.TemperatureRange);

        builder.HasMany(r => r.Locations).WithOne().HasForeignKey(l => l.RoomId);

        builder.HasMany(r => r.Locations)
            .WithOne(l => l.Room)    
            .HasForeignKey(l => l.RoomId);
    }
}