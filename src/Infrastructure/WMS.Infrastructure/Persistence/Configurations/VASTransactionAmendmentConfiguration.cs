using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;

namespace WMS.Infrastructure.Persistence.Configurations;

public class VASTransactionAmendmentConfiguration : IEntityTypeConfiguration<VASTransactionAmendment>
{
    public void Configure(EntityTypeBuilder<VASTransactionAmendment> builder)
    {
        builder.ToTable("VASTransactionAmendments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Reason)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(a => a.AmendmentDetails)
            .IsRequired();

        builder.Property(a => a.AmendmentType)
            .IsRequired();

        builder.Property(a => a.Timestamp)
            .IsRequired();

        // Relationships
        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // Index for querying amendments by transaction
        builder.HasIndex(a => a.OriginalTransactionId);

        // Index for querying by amendment type
        builder.HasIndex(a => a.AmendmentType);
    }
}
