using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.TaxId).HasMaxLength(50);

        builder.OwnsOne(c => c.Address);

        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        
        builder.Property(c => c.Gs1CompanyPrefix).HasMaxLength(20).HasDefaultValue("0000000");
        builder.Property(c => c.DefaultBarcodeFormat).HasMaxLength(20).HasDefaultValue("SSCC-18");
    }
}