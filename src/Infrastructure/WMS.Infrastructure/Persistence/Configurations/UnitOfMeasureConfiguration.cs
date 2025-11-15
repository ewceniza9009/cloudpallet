using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Configurations;

public class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.ToTable("UnitsOfMeasure");

        builder.HasKey(uom => uom.Id);

        builder.Property(uom => uom.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(uom => uom.Symbol)
            .IsRequired()
            .HasMaxLength(10);

        // Ensures that each UOM symbol (e.g., "KG", "LB", "EACH") is unique.
        builder.HasIndex(uom => uom.Symbol).IsUnique();
    }
}