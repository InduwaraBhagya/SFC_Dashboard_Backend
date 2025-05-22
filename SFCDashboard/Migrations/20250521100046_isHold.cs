using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFCDashboard.Migrations
{
    /// <inheritdoc />
    public partial class isHold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHold",
                table: "PlannedEvents",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHold",
                table: "PlannedEvents");
        }
    }
}
