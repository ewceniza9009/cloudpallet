using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(i => i.InvoiceNumber).IsUnique();

        builder.HasMany(i => i.Lines)
          .WithOne()
          .HasForeignKey(il => il.InvoiceId);

        builder.HasOne(i => i.Account)
          .WithMany()
          .HasForeignKey(i => i.AccountId)         
          .OnDelete(DeleteBehavior.Restrict);

    }
}