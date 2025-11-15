using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Aggregates.Warehouse;

namespace WMS.Infrastructure.Persistence.Configurations;

public class DockConfiguration : IEntityTypeConfiguration<Dock>
{
    public void Configure(EntityTypeBuilder<Dock> builder)
    {
        builder.ToTable("Docks");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name).IsRequired().HasMaxLength(50);

        builder.Property(d => d.CurrentAppointmentId).IsRequired(false);

        builder.HasOne<Warehouse>()
            .WithMany(w => w.Docks)
            .HasForeignKey(d => d.WarehouseId);
    }
}