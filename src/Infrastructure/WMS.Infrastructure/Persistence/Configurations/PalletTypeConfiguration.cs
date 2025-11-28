using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Configurations;

public class PalletTypeConfiguration : IEntityTypeConfiguration<PalletType>
{
    public void Configure(EntityTypeBuilder<PalletType> builder)
    {
        builder.ToTable("PalletTypes");
        builder.HasKey(pt => pt.Id);

        builder.Property(pt => pt.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(pt => pt.Name).IsUnique();

        builder.Property(pt => pt.TareWeight).HasPrecision(DecimalPrecision.QuantityPrecision, DecimalPrecision.QuantityScale);       
        builder.Property(pt => pt.Length).HasPrecision(DecimalPrecision.QuantityPrecision, DecimalPrecision.QuantityScale);       
        builder.Property(pt => pt.Width).HasPrecision(DecimalPrecision.QuantityPrecision, DecimalPrecision.QuantityScale);
        builder.Property(pt => pt.Height).HasPrecision(DecimalPrecision.QuantityPrecision, DecimalPrecision.QuantityScale);
    }
}