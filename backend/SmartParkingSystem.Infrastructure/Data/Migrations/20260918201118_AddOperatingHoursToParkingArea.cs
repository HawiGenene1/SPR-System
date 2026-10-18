using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartParkingSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOperatingHoursToParkingArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OperatingHours",
                table: "ParkingAreas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "24/7");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OperatingHours",
                table: "ParkingAreas");
        }
    }
}
