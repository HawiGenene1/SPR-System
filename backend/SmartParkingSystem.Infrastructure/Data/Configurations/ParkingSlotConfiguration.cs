using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartParkingSystem.Domain.Entities;

namespace SmartParkingSystem.Infrastructure.Data.Configurations;

public class ParkingSlotConfiguration : IEntityTypeConfiguration<ParkingSlot>
{
    public void Configure(EntityTypeBuilder<ParkingSlot> builder)
    {
        builder.ToTable("ParkingSlots");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SlotNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(s => s.FloorNumber)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(s => s.SlotType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(s => s.VehicleType)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(SmartParkingSystem.Domain.Enums.VehicleType.Car);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(s => s.HourlyRate)
            .HasPrecision(18, 2);

        builder.Property(s => s.IsActive)
            .HasDefaultValue(true);

        // Crucial constraint: SlotNumber must be unique within a ParkingArea
        builder.HasIndex(s => new { s.ParkingAreaId, s.SlotNumber })
            .IsUnique();

        // Index on status for rapid availability searches
        builder.HasIndex(s => new { s.ParkingAreaId, s.Status });

        builder.HasOne(s => s.ParkingArea)
            .WithMany(p => p.ParkingSlots)
            .HasForeignKey(s => s.ParkingAreaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
