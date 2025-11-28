using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;

namespace WMS.Infrastructure.Persistence.Configurations;

public class PickTransactionConfiguration : IEntityTypeConfiguration<PickTransaction>
{
    public void Configure(EntityTypeBuilder<PickTransaction> builder)
    {
        builder.ToTable("PickTransactions");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PickWeight).HasPrecision(DecimalPrecision.QuantityPrecision, DecimalPrecision.QuantityScale);
        builder.Property(p => p.BatchNumber).HasMaxLength(100).IsRequired(false);
        builder.Property(p => p.ExpiryDate).IsRequired(false);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne(p => p.MaterialInventory)
            .WithMany()
            .HasForeignKey(p => p.InventoryId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Account)
            .WithMany()
            .HasForeignKey(p => p.AccountId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.IsExpedited)
               .IsRequired()
               .HasDefaultValue(false);     
    }
}