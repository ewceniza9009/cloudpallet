using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Configurations;

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("InvoiceLines");
        builder.HasKey(il => il.Id);

        builder.Property(il => il.Description).HasMaxLength(200);
        builder.Property(il => il.Tier).HasMaxLength(50);
        builder.Property(il => il.Amount).HasPrecision(DecimalPrecision.MoneyPrecision, DecimalPrecision.MoneyScale);
        builder.Property(il => il.UnitRate).HasPrecision(DecimalPrecision.UnitCostPrecision, DecimalPrecision.UnitCostScale);
    }
}