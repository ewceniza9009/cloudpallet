using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Persistence.Configurations;

public class InventoryAdjustmentConfiguration : IEntityTypeConfiguration<InventoryAdjustment>
{
    public void Configure(EntityTypeBuilder<InventoryAdjustment> builder)
    {
        builder.ToTable("InventoryAdjustments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.DeltaQuantity)
            .HasPrecision(DecimalPrecision.QuantityPrecision, DecimalPrecision.QuantityScale)       
            .IsRequired();

        builder.Property(a => a.Reason)
            .HasConversion<string>()     
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(a => a.Timestamp).IsRequired();

        builder.HasOne(a => a.Inventory)
            .WithMany()       
            .HasForeignKey(a => a.InventoryId)
            .OnDelete(DeleteBehavior.Restrict);       

        builder.HasOne(a => a.Account)
            .WithMany()       
            .HasForeignKey(a => a.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.User)
            .WithMany()       
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}