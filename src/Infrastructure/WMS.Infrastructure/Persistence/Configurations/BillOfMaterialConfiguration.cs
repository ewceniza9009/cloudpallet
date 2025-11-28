using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Configurations;

public class BillOfMaterialConfiguration : IEntityTypeConfiguration<BillOfMaterial>
{
    public void Configure(EntityTypeBuilder<BillOfMaterial> builder)
    {
        builder.ToTable("BillsOfMaterial");
        builder.HasKey(b => b.Id);

        builder.HasIndex(b => b.OutputMaterialId).IsUnique();

        builder.Property(b => b.OutputQuantity).HasPrecision(DecimalPrecision.QuantityPrecision, DecimalPrecision.QuantityScale);

        builder.HasMany(b => b.Lines)
            .WithOne()
            .HasForeignKey(l => l.BillOfMaterialId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Material>()
            .WithMany()
            .HasForeignKey(b => b.OutputMaterialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}