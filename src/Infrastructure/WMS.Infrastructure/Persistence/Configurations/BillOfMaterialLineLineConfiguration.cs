using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Configurations;

public class BillOfMaterialLineConfiguration : IEntityTypeConfiguration<BillOfMaterialLine>
{
    public void Configure(EntityTypeBuilder<BillOfMaterialLine> builder)
    {
        builder.ToTable("BillOfMaterialLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.InputQuantity).HasPrecision(DecimalPrecision.QuantityPrecision, DecimalPrecision.QuantityScale);

        builder.HasOne<Material>()
            .WithMany()
            .HasForeignKey(l => l.InputMaterialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}