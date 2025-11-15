using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;

namespace WMS.Infrastructure.Persistence.Configurations;

public class PalletConfiguration : IEntityTypeConfiguration<Pallet>
{
    public void Configure(EntityTypeBuilder<Pallet> builder)
    {
        builder.ToTable("Pallets");
        builder.HasKey(p => p.Id);

        builder.HasMany(p => p.Lines)
            .WithOne(l => l.Pallet)
            .HasForeignKey(l => l.PalletId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.Barcode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.Barcode).IsUnique();

        builder.Property(p => p.Status)
               .HasConversion<string>()
               .HasMaxLength(30);       

        builder.HasOne(p => p.PalletType)
            .WithMany()
            .HasForeignKey(p => p.PalletTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Account)
            .WithMany()
            .HasForeignKey(p => p.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.IsCrossDock)
               .IsRequired()
               .HasDefaultValue(false);     
    }
}