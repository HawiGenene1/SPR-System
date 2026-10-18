using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartParkingSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleTypeAndHourlyRateToParkingSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRate",
                table: "ParkingSlots",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VehicleType",
                table: "ParkingSlots",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "ParkingSlots");

            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "ParkingSlots");
        }
    }
}
