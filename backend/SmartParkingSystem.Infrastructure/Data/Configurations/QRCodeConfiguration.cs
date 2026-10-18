using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartParkingSystem.Domain.Entities;

namespace SmartParkingSystem.Infrastructure.Data.Configurations;

public class QRCodeConfiguration : IEntityTypeConfiguration<QRCode>
{
    public void Configure(EntityTypeBuilder<QRCode> builder)
    {
        builder.ToTable("QRCodes");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Code)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(q => q.Code)
            .IsUnique();

        builder.Property(q => q.ImageData)
            .HasColumnType("text");

        builder.Property(q => q.IsUsedForEntry)
            .HasDefaultValue(false);

        builder.Property(q => q.IsUsedForExit)
            .HasDefaultValue(false);

        builder.Property(q => q.ExpiresAt)
            .IsRequired();

        builder.HasQueryFilter(q => !q.IsDeleted);
    }
}
