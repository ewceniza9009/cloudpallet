using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;

namespace WMS.Infrastructure.Persistence.Configurations;

public class VASTransactionConfiguration : IEntityTypeConfiguration<VASTransaction>
{
    public void Configure(EntityTypeBuilder<VASTransaction> builder)
    {
        builder.ToTable("VASTransactions");

        builder.HasKey(vt => vt.Id);

        // Define the single one-to-many relationship using the private backing field name
        builder.HasMany<VASTransactionLine>("_lines")
            .WithOne(vl => vl.VASTransaction)
            .HasForeignKey(vl => vl.VASTransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Tell EF to IGNORE the public properties, as they are unmapped "views"
        builder.Ignore(vt => vt.InputLines);
        builder.Ignore(vt => vt.OutputLines);

        builder.HasOne(vt => vt.Account)
            .WithMany()
            .HasForeignKey(vt => vt.AccountId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(vt => vt.User)
            .WithMany()
            .HasForeignKey(vt => vt.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // Void tracking fields
        builder.Property(vt => vt.IsVoided)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(vt => vt.VoidedAt);
        builder.Property(vt => vt.VoidedByUserId);
        builder.Property(vt => vt.VoidReason).HasMaxLength(500);

        // Index for querying non-voided transactions
        builder.HasIndex(vt => vt.IsVoided);

        // Amendments relationship
        builder.HasMany<VASTransactionAmendment>("_amendments")
            .WithOne(a => a.OriginalTransaction)
            .HasForeignKey(a => a.OriginalTransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(vt => vt.Amendments);
    }
}