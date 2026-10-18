using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartParkingSystem.Domain.Entities;

namespace SmartParkingSystem.Infrastructure.Data.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReservationNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(r => r.ReservationNumber)
            .IsUnique();

        builder.Property(r => r.StartTime)
            .IsRequired();

        builder.Property(r => r.EndTime)
            .IsRequired();

        builder.Property(r => r.TotalPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(r => r.CancellationReason)
            .HasMaxLength(250);

        // Crucial index for fast collision/overlap detection
        builder.HasIndex(r => new { r.ParkingSlotId, r.StartTime, r.EndTime, r.Status });
        builder.HasIndex(r => new { r.UserId, r.Status });

        builder.HasOne(r => r.User)
            .WithMany(u => u.Reservations)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ParkingSlot)
            .WithMany(s => s.Reservations)
            .HasForeignKey(r => r.ParkingSlotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Vehicle)
            .WithMany(v => v.Reservations)
            .HasForeignKey(r => r.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Payment)
            .WithOne(p => p.Reservation)
            .HasForeignKey<Payment>(p => p.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.QRCode)
            .WithOne(q => q.Reservation)
            .HasForeignKey<QRCode>(q => q.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.ParkingHistory)
            .WithOne(h => h.Reservation)
            .HasForeignKey<ParkingHistory>(h => h.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
