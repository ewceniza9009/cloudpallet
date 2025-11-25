using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;

namespace WMS.Infrastructure.Persistence.Configurations;

public class VASTransactionLineConfiguration : IEntityTypeConfiguration<VASTransactionLine>
{
    public void Configure(EntityTypeBuilder<VASTransactionLine> builder)
    {
        builder.ToTable("VASTransactionLines");
        builder.HasKey(vl => vl.Id);

        builder.Property(vl => vl.Quantity).HasPrecision(12, 3).IsRequired();
        builder.Property(vl => vl.IsInput).IsRequired();

        // Amendment tracking fields
        builder.Property(vl => vl.OriginalQuantity).HasPrecision(12, 3);
        builder.Property(vl => vl.OriginalWeight).HasPrecision(12, 3);
        builder.Property(vl => vl.IsAmended)
            .IsRequired()
            .HasDefaultValue(false);
        builder.Property(vl => vl.AmendedAt);

        // Foreign key to the Material that was consumed or produced
        builder.HasOne(vl => vl.Material)
            .WithMany()
            .HasForeignKey(vl => vl.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}