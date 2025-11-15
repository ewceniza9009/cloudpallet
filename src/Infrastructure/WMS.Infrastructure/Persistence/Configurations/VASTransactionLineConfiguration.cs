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

        // Foreign key to the Material that was consumed or produced
        builder.HasOne<Material>()
            .WithMany()
            .HasForeignKey(vl => vl.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        // Assuming a hidden foreign key links this to VASTransaction (if VASTransaction was modeled as a standard entity, 
        // the foreign key would be explicit here, but since it is an aggregate root, we keep it simple for now).
    }
}