// ---- File: src/Infrastructure/WMS.Infrastructure/Persistence/Configurations/UserConfiguration.cs ----
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // This configuration is now simpler because Identity handles most of it.
        // We only need to configure our custom properties.

        builder.Property(u => u.FirstName).HasMaxLength(50);
        builder.Property(u => u.LastName).HasMaxLength(50);

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(u => u.CompanyId);
    }
}