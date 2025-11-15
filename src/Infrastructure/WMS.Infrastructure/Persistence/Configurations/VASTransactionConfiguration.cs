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

        // --- START: MODIFICATION ---

        // 1. Define the single one-to-many relationship using the private backing field name
        // This tells EF that the collection is named "_lines" inside the VASTransaction class.
        builder.HasMany<VASTransactionLine>("_lines") // <-- Use the private field name
            .WithOne(vl => vl.VASTransaction)
            .HasForeignKey(vl => vl.VASTransactionId)
            .OnDelete(DeleteBehavior.Cascade); // Explicitly set cascade behavior

        // 2. Tell EF to IGNORE the public properties, as they are unmapped "views"
        builder.Ignore(vt => vt.InputLines);
        builder.Ignore(vt => vt.OutputLines);

        // --- END: MODIFICATION ---

        builder.HasOne(vt => vt.Account)
            .WithMany()
            .HasForeignKey(vt => vt.AccountId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(vt => vt.User)
            .WithMany()
            .HasForeignKey(vt => vt.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}