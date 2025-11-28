using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Configurations;

public class RateConfiguration : IEntityTypeConfiguration<Rate>
{
    public void Configure(EntityTypeBuilder<Rate> builder)
    {
        builder.ToTable("Rates");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Value).HasPrecision(DecimalPrecision.UnitCostPrecision, DecimalPrecision.UnitCostScale);
    }
}