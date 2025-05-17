using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFCDashboard.Migrations
{
    /// <inheritdoc />
    public partial class updatepeTasktable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TaskPhase",
                table: "PETasks",
                newName: "Priority");

            migrationBuilder.AlterColumn<string>(
                name: "PENumber",
                table: "PETasks",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EstimatedTime",
                table: "PETasks",
                type: "time",
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
                name: "EstimatedTime",
                table: "PETasks");

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

            migrationBuilder.AlterColumn<string>(
                name: "PENumber",
                table: "PETasks",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
