using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities.Transaction;

namespace WMS.Infrastructure.Persistence.Configurations;

public class PutawayTransactionConfiguration : IEntityTypeConfiguration<PutawayTransaction>
{
    public void Configure(EntityTypeBuilder<PutawayTransaction> builder)
    {
        builder.ToTable("PutawayTransactions");
        builder.HasKey(pt => pt.Id);
        builder.HasIndex(pt => pt.PalletId).IsUnique();

        builder.HasOne(pt => pt.User)
            .WithMany()
            .HasForeignKey(pt => pt.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pt => pt.Pallet)
            .WithMany()
            .HasForeignKey(pt => pt.PalletId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}