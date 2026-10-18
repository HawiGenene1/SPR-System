using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartParkingSystem.Domain.Entities;

namespace SmartParkingSystem.Infrastructure.Data.Configurations;

public class ParkingAreaConfiguration : IEntityTypeConfiguration<ParkingArea>
{
    public void Configure(EntityTypeBuilder<ParkingArea> builder)
    {
        builder.ToTable("ParkingAreas");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Description)
            .HasMaxLength(250);

        builder.Property(p => p.Address)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(p => p.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Latitude)
            .HasPrecision(9, 6);

        builder.Property(p => p.Longitude)
            .HasPrecision(9, 6);

        builder.Property(p => p.HourlyRate)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.OperatingHours)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("24/7");

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(p => new { p.City, p.Name });

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
