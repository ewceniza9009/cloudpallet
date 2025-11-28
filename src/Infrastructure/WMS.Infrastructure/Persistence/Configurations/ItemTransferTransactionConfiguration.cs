using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.Transaction;

namespace WMS.Infrastructure.Persistence.Configurations;

public class ItemTransferTransactionConfiguration : IEntityTypeConfiguration<ItemTransferTransaction>
{
    public void Configure(EntityTypeBuilder<ItemTransferTransaction> builder)
    {
        builder.ToTable("ItemTransferTransactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.QuantityTransferred).HasPrecision(DecimalPrecision.QuantityPrecision, DecimalPrecision.QuantityScale);
        builder.Property(t => t.WeightTransferred).HasPrecision(DecimalPrecision.QuantityPrecision, DecimalPrecision.QuantityScale);

        builder.HasOne(t => t.SourceInventory)
            .WithMany()
            .HasForeignKey(t => t.SourceInventoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.NewDestinationPallet)
            .WithMany()
            .HasForeignKey(t => t.NewDestinationPalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}