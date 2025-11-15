using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Persistence.Configurations;

public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.ToTable("Materials");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).IsRequired().HasMaxLength(100);

        builder.Property(m => m.Sku).IsRequired().HasMaxLength(50);
        builder.HasIndex(m => m.Sku).IsUnique();

        builder.Property(m => m.Description).HasMaxLength(500);

        builder.Property(m => m.RequiredTempZone).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.MaterialType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(MaterialType.Normal);
        builder.HasOne<MaterialCategory>()
            .WithMany()
            .HasForeignKey(m => m.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(m => m.UomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}