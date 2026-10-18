using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartParkingSystem.Domain.Entities;

namespace SmartParkingSystem.Infrastructure.Data.Configurations;

public class ParkingHistoryConfiguration : IEntityTypeConfiguration<ParkingHistory>
{
    public void Configure(EntityTypeBuilder<ParkingHistory> builder)
    {
        builder.ToTable("ParkingHistories");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.CheckInTime)
            .IsRequired();

        builder.Property(h => h.OverstayFee)
            .HasPrecision(18, 2);

        builder.Property(h => h.Notes)
            .HasMaxLength(500);

        builder.HasIndex(h => new { h.ParkingSlotId, h.CheckInTime, h.CheckOutTime });

        builder.HasOne(h => h.Vehicle)
            .WithMany(v => v.ParkingHistories)
            .HasForeignKey(h => h.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.ParkingSlot)
            .WithMany(s => s.ParkingHistories)
            .HasForeignKey(h => h.ParkingSlotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.AttendantCheckIn)
            .WithMany(u => u.CheckInsHandled)
            .HasForeignKey(h => h.AttendantCheckInId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(h => h.AttendantCheckOut)
            .WithMany(u => u.CheckOutsHandled)
            .HasForeignKey(h => h.AttendantCheckOutId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasQueryFilter(h => !h.IsDeleted);
    }
}
