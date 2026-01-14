using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurfPOS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Shifts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReportFilePath",
                table: "Shifts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TransactionCount",
                table: "Shifts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "ReportFilePath",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "TransactionCount",
                table: "Shifts");
        }
    }
}
