using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFCDashboard.Migrations
{
    /// <inheritdoc />
    public partial class add : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Escalations_Users_RecipientId",
                table: "Escalations");

            migrationBuilder.DropForeignKey(
                name: "FK_UserWorkGroups_WorkGroups_WorkGroupId1",
                table: "UserWorkGroups");

            migrationBuilder.DropTable(
                name: "TaskEscalations");

            migrationBuilder.DropIndex(
                name: "IX_UserWorkGroups_WorkGroupId1",
                table: "UserWorkGroups");

            migrationBuilder.DropIndex(
                name: "IX_Escalations_RecipientId",
                table: "Escalations");

            migrationBuilder.DropColumn(
                name: "WorkGroupId1",
                table: "UserWorkGroups");

            migrationBuilder.DropColumn(
                name: "OLAViolationTime",
                table: "Escalations");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDateFromPE",
                table: "PlannedEvents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ViolationStartTime",
                table: "PETasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReminder",
                table: "PEIssues",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "Level",
                table: "Escalations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "IgnoreReason",
                table: "Escalations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IsResolved",
                table: "Escalations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Message",
                table: "Escalations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PlannedEventId",
                table: "Escalations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Escalations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Escalations_PlannedEventId",
                table: "Escalations",
                column: "PlannedEventId");

            migrationBuilder.AddForeignKey(
                name: "FK_Escalations_PlannedEvents_PlannedEventId",
                table: "Escalations",
                column: "PlannedEventId",
                principalTable: "PlannedEvents",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Escalations_PlannedEvents_PlannedEventId",
                table: "Escalations");

            migrationBuilder.DropIndex(
                name: "IX_Escalations_PlannedEventId",
                table: "Escalations");

            migrationBuilder.DropColumn(
                name: "CreatedDateFromPE",
                table: "PlannedEvents");

            migrationBuilder.DropColumn(
                name: "ViolationStartTime",
                table: "PETasks");

            migrationBuilder.DropColumn(
                name: "IsReminder",
                table: "PEIssues");

            migrationBuilder.DropColumn(
                name: "IsResolved",
                table: "Escalations");

            migrationBuilder.DropColumn(
                name: "Message",
                table: "Escalations");

            migrationBuilder.DropColumn(
                name: "PlannedEventId",
                table: "Escalations");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Escalations");

            migrationBuilder.AddColumn<int>(
                name: "WorkGroupId1",
                table: "UserWorkGroups",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Level",
                table: "Escalations",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IgnoreReason",
                table: "Escalations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OLAViolationTime",
                table: "Escalations",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "TaskEscalations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EscalatedToUserId = table.Column<int>(type: "int", nullable: false),
                    PlannedEventId = table.Column<int>(type: "int", nullable: false),
                    EscalationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EscalationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolutionComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResolvedTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskEscalations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskEscalations_PlannedEvents_PlannedEventId",
                        column: x => x.PlannedEventId,
                        principalTable: "PlannedEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskEscalations_Users_EscalatedToUserId",
                        column: x => x.EscalatedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserWorkGroups_WorkGroupId1",
                table: "UserWorkGroups",
                column: "WorkGroupId1");

            migrationBuilder.CreateIndex(
                name: "IX_Escalations_RecipientId",
                table: "Escalations",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskEscalations_EscalatedToUserId",
                table: "TaskEscalations",
                column: "EscalatedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskEscalations_PlannedEventId",
                table: "TaskEscalations",
                column: "PlannedEventId");

            migrationBuilder.AddForeignKey(
                name: "FK_Escalations_Users_RecipientId",
                table: "Escalations",
                column: "RecipientId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserWorkGroups_WorkGroups_WorkGroupId1",
                table: "UserWorkGroups",
                column: "WorkGroupId1",
                principalTable: "WorkGroups",
                principalColumn: "Id");
        }
    }
}
