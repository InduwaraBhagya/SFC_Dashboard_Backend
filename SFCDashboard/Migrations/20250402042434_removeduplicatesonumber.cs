using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFCDashboard.Migrations
{
    /// <inheritdoc />
    public partial class Removeduplicatesonumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SONumber",
                table: "PlannedEvents");

            migrationBuilder.RenameColumn(
                name: "SoNumber",
                table: "PlannedEvents",
                newName: "SONumber");

            migrationBuilder.AlterColumn<string>(
                name: "SONumber",
                table: "PlannedEvents",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SONumber",
                table: "PlannedEvents",
                newName: "SoNumber");

            migrationBuilder.AlterColumn<string>(
                name: "SoNumber",
                table: "PlannedEvents",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "SONumber",
                table: "PlannedEvents",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
