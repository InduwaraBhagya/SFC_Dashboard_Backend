using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFCDashboard.Migrations
{
    /// <inheritdoc />
    public partial class IsHiddenFromInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHiddenFromInbox",
                table: "PEIssues",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHiddenFromInbox",
                table: "PEIssues");
        }
    }
}
