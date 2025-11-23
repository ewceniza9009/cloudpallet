using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;
using WMS.Domain.Enums;     

namespace WMS.Infrastructure.Persistence.Configurations;

public class MaterialInventoryConfiguration : IEntityTypeConfiguration<MaterialInventory>
{
    public void Configure(EntityTypeBuilder<MaterialInventory> builder)
    {
        builder.ToTable("MaterialInventories");
        builder.HasKey(mi => mi.Id);

        builder.HasIndex(mi => mi.PalletLineId).IsUnique();
        builder.HasIndex(mi => mi.Barcode); // Optimized for LPN search

        builder.OwnsOne(mi => mi.WeightActual);

        builder.HasOne(mi => mi.Material)
            .WithMany()
            .HasForeignKey(mi => mi.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(mi => mi.Location)
            .WithMany()
            .HasForeignKey(mi => mi.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(mi => mi.Pallet)
            .WithMany(p => p.Inventory)
            .HasForeignKey(mi => mi.PalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(mi => mi.ComplianceLabelStatus)
               .HasConversion<string>()        
               .HasMaxLength(30)            
               .HasDefaultValue(ComplianceLabelType.None);    

        builder.Property(mi => mi.QuarantineStartDate).IsRequired(false);
        builder.Property(mi => mi.QuarantineEndDate).IsRequired(false);
        builder.Property(mi => mi.Status)
              .HasConversion<string>()
              .HasMaxLength(30)
              .HasDefaultValue(InventoryStatus.Available);

    }
}