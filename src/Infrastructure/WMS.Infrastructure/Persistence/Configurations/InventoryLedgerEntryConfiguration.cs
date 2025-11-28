using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Persistence.Configurations;

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
        builder.HasIndex(le => le.TransactionId);
        builder.HasIndex(le => le.LocationId);
        builder.HasIndex(le => le.PalletId);
        builder.HasIndex(le => le.SupplierId);
        builder.HasIndex(le => le.TruckId);
        builder.HasIndex(le => le.UserId);

        // --- Property Configurations ---
        builder.Property(le => le.Timestamp).IsRequired();
        builder.Property(le => le.MaterialId).IsRequired();
        builder.Property(le => le.AccountId).IsRequired();

        builder.Property(le => le.TransactionType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(le => le.TransactionId).IsRequired();

        builder.Property(le => le.QuantityChange)
            .HasPrecision(DecimalPrecision.QuantityPrecision, DecimalPrecision.QuantityScale)
            .IsRequired();

        builder.Property(le => le.WeightChange)
            .HasPrecision(DecimalPrecision.QuantityPrecision, DecimalPrecision.QuantityScale)
            .IsRequired();

        builder.Property(le => le.MaterialName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(le => le.AccountName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(le => le.DocumentReference)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(le => le.UserName)
            .HasMaxLength(100)
            .IsRequired(false);
    }
}