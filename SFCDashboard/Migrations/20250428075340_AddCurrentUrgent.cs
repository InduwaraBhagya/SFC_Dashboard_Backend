using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFCDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentUrgent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TaskPhase",
                table: "PETasks",
                newName: "Priority");

            migrationBuilder.AddColumn<int>(
                name: "CurrentUrgentTaskId",
                table: "PlannedEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsUrgent",
                table: "PETasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UrgentRequested",
                table: "PETasks",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentUrgentTaskId",
                table: "PlannedEvents");

            migrationBuilder.DropColumn(
                name: "IsUrgent",
                table: "PETasks");

            migrationBuilder.DropColumn(
                name: "UrgentRequested",
                table: "PETasks");

            migrationBuilder.RenameColumn(
                name: "Priority",
                table: "PETasks",
                newName: "TaskPhase");
        }
    }
}
