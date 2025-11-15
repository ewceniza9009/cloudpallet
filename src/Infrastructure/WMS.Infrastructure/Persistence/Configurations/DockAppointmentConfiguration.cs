using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Aggregates.DockAppointment;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Configurations;

public class DockAppointmentConfiguration : IEntityTypeConfiguration<DockAppointment>
{
    public void Configure(EntityTypeBuilder<DockAppointment> builder)
    {
        builder.ToTable("DockAppointments");
        builder.HasKey(da => da.Id);

        builder.Property(da => da.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(da => new { da.DockId, da.StartDateTime, da.EndDateTime });

        builder.HasOne(da => da.Truck)
            .WithMany()
            .HasForeignKey(da => da.TruckId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(da => da.Dock)
            .WithMany()
            .HasForeignKey(da => da.DockId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(da => da.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(da => da.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}