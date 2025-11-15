using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;

namespace WMS.Infrastructure.Persistence.Configurations;

public class WithdrawalTransactionConfiguration : IEntityTypeConfiguration<WithdrawalTransaction>
{
    public void Configure(EntityTypeBuilder<WithdrawalTransaction> builder)
    {
        builder.ToTable("WithdrawalTransactions");
        builder.HasKey(wt => wt.Id);

        builder.Property(wt => wt.ShipmentNumber).IsRequired().HasMaxLength(100);

        builder.HasMany(wt => wt.Picks)
            .WithMany(p => p.WithdrawalTransactions);

        builder.HasOne(wt => wt.Appointment)
            .WithMany()
            .HasForeignKey(wt => wt.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(wt => wt.Account)
            .WithMany()
            .HasForeignKey(wt => wt.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}