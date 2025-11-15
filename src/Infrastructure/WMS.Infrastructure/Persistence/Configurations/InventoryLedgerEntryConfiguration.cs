// ---- File: src/Infrastructure/WMS.Infrastructure/Persistence/Configurations/InventoryLedgerEntryConfiguration.cs ----
// [COMPLETE NEW FILE]

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities; // For InventoryLedgerEntry
using WMS.Domain.Enums;   // For LedgerTransactionType

namespace WMS.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the InventoryLedgerEntry entity.
/// </summary>
public class InventoryLedgerEntryConfiguration : IEntityTypeConfiguration<InventoryLedgerEntry>
{
    public void Configure(EntityTypeBuilder<InventoryLedgerEntry> builder)
    {
        builder.ToTable("InventoryLedgerEntries");

        builder.HasKey(le => le.Id);

        // --- Indexes for efficient filtering ---
        builder.HasIndex(le => le.Timestamp);
        builder.HasIndex(le => le.MaterialId);
        builder.HasIndex(le => le.AccountId);
        builder.HasIndex(le => le.TransactionType);
        builder.HasIndex(le => le.TransactionId); // Useful for debugging/tracing
        builder.HasIndex(le => le.LocationId);    // Nullable index
        builder.HasIndex(le => le.PalletId);       // Nullable index
        builder.HasIndex(le => le.SupplierId);     // Nullable index
        builder.HasIndex(le => le.TruckId);        // Nullable index
        builder.HasIndex(le => le.UserId);         // Nullable index
        // Consider composite indexes if queries often combine specific fields, e.g., (MaterialId, AccountId, Timestamp)
        // builder.HasIndex(le => new { le.MaterialId, le.AccountId, le.Timestamp });

        // --- Property Configurations ---
        builder.Property(le => le.Timestamp).IsRequired();

        builder.Property(le => le.MaterialId).IsRequired();

        builder.Property(le => le.AccountId).IsRequired();

        builder.Property(le => le.TransactionType)
            .HasConversion<string>() // Store enum as string for readability
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(le => le.TransactionId).IsRequired();

        builder.Property(le => le.QuantityChange)
            .HasPrecision(18, 5) // Allow precision for fractional quantities
            .IsRequired();

        builder.Property(le => le.WeightChange)
            .HasPrecision(18, 5)   // Allow precision for weight
            .IsRequired();

        builder.Property(le => le.MaterialName)
            .HasMaxLength(150) // Adjust length as needed based on Material.Name
            .IsRequired();

        builder.Property(le => le.AccountName)
            .HasMaxLength(150)  // Adjust length as needed based on Account.Name
            .IsRequired();

        builder.Property(le => le.DocumentReference)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(le => le.UserName)
            .HasMaxLength(100) // Match User First/Last Name lengths
            .IsRequired(false); // Allow null for system actions

        // Optional Foreign Keys (can help data integrity but add slight overhead)
        // Leaving them commented out for a pure read model approach.
        // builder.HasOne<Material>().WithMany().HasForeignKey(le => le.MaterialId).OnDelete(DeleteBehavior.Restrict);
        // builder.HasOne<Account>().WithMany().HasForeignKey(le => le.AccountId).OnDelete(DeleteBehavior.Restrict);
        // builder.HasOne<Location>().WithMany().HasForeignKey(le => le.LocationId).OnDelete(DeleteBehavior.Restrict);
        // ... etc ...
    }
}