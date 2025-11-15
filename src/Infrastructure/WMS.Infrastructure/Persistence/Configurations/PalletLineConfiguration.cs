using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;

namespace WMS.Infrastructure.Persistence.Configurations;

public class PalletLineConfiguration : IEntityTypeConfiguration<PalletLine>
{
    public void Configure(EntityTypeBuilder<PalletLine> builder)
    {
        builder.ToTable("PalletLines");
        builder.HasKey(pl => pl.Id);

        builder.Property(pl => pl.BatchNumber).HasMaxLength(50);
        builder.Property(pl => pl.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(pl => pl.Barcode).HasMaxLength(50);

        builder.HasOne(pl => pl.Material)
            .WithMany()
            .HasForeignKey(pl => pl.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pl => pl.Pallet)
            .WithMany(p => p.Lines)
            .HasForeignKey(pl => pl.PalletId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}