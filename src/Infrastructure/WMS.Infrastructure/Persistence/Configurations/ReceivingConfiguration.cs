using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Aggregates.DockAppointment;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;

namespace WMS.Infrastructure.Persistence.Configurations;

public class ReceivingConfiguration : IEntityTypeConfiguration<Receiving>
{
    public void Configure(EntityTypeBuilder<Receiving> builder)
    {
        builder.ToTable("Receivings");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.TotalPallets);

        builder.HasMany(r => r.Pallets)
            .WithOne(p => p.Receiving)
            .HasForeignKey(p => p.ReceivingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Supplier)
            .WithMany()
            .HasForeignKey(r => r.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Appointment)
            .WithMany()
            .HasForeignKey(r => r.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Account)
            .WithMany()
            .HasForeignKey(r => r.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.CreatedByUser)
            .WithMany()
            .HasForeignKey(r => r.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}