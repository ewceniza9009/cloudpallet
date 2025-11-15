using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Aggregates.Warehouse;

namespace WMS.Infrastructure.Persistence.Configurations;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");
        builder.HasKey(w => w.Id);

        builder.OwnsOne(w => w.Address);

        builder.HasMany(w => w.Rooms).WithOne().HasForeignKey(r => r.WarehouseId);
        builder.HasMany(w => w.Docks).WithOne().HasForeignKey(d => d.WarehouseId);
        builder.HasMany(w => w.YardSpots).WithOne().HasForeignKey(y => y.WarehouseId);
    }
}