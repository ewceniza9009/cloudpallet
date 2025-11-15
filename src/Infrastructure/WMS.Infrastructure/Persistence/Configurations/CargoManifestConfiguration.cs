using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Aggregates.Cargo;

namespace WMS.Infrastructure.Persistence.Configurations;

public class CargoManifestConfiguration : IEntityTypeConfiguration<CargoManifest>
{
    public void Configure(EntityTypeBuilder<CargoManifest> builder)
    {
        builder.ToTable("CargoManifests");
        builder.HasKey(cm => cm.Id);

        builder.OwnsMany(cm => cm.Lines, lineBuilder =>
        {
            lineBuilder.ToTable("CargoManifestLines");
            lineBuilder.WithOwner().HasForeignKey("CargoManifestId");
            lineBuilder.HasKey(l => l.Id);
        });
    }
}