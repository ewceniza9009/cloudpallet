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

        builder.Property(pt => pt.TareWeight).HasPrecision(10, 3);       
        builder.Property(pt => pt.Length).HasPrecision(10, 2);       
        builder.Property(pt => pt.Width).HasPrecision(10, 2);
        builder.Property(pt => pt.Height).HasPrecision(10, 2);

    }
}