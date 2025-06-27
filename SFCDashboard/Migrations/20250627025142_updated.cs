using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFCDashboard.Migrations
{
    /// <inheritdoc />
    public partial class updated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UrgentRequestedById",
                table: "PlannedEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UrgentRequestedByName",
                table: "PlannedEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AreaNetworkEngineers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Area = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EngineerName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AreaNetworkEngineers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RTOMWeights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RTOM = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RTOMWeights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SurveyTaskActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PETaskId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SystemUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyTaskActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyTaskActivities_PETasks_PETaskId",
                        column: x => x.PETaskId,
                        principalTable: "PETasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SurveyTaskActivities_Users_SystemUserId",
                        column: x => x.SystemUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UDCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UDCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UDSubCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    SubCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UDSubCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UDSubCategories_UDCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "UDCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UDNames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    SubCategoryId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UDNames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UDNames_UDCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "UDCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UDNames_UDSubCategories_SubCategoryId",
                        column: x => x.SubCategoryId,
                        principalTable: "UDSubCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BOQs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    SubCategoryId = table.Column<int>(type: "int", nullable: false),
                    UDNameId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AdjustedUnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BOQs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BOQs_PETasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "PETasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BOQs_UDCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "UDCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BOQs_UDNames_UDNameId",
                        column: x => x.UDNameId,
                        principalTable: "UDNames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BOQs_UDSubCategories_SubCategoryId",
                        column: x => x.SubCategoryId,
                        principalTable: "UDSubCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BOQs_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BOQs_CategoryId",
                table: "BOQs",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BOQs_CreatedByUserId",
                table: "BOQs",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BOQs_SubCategoryId",
                table: "BOQs",
                column: "SubCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BOQs_TaskId",
                table: "BOQs",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_BOQs_UDNameId",
                table: "BOQs",
                column: "UDNameId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyTaskActivities_PETaskId",
                table: "SurveyTaskActivities",
                column: "PETaskId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyTaskActivities_SystemUserId",
                table: "SurveyTaskActivities",
                column: "SystemUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UDNames_CategoryId",
                table: "UDNames",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UDNames_SubCategoryId",
                table: "UDNames",
                column: "SubCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UDSubCategories_CategoryId",
                table: "UDSubCategories",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AreaNetworkEngineers");

            migrationBuilder.DropTable(
                name: "BOQs");

            migrationBuilder.DropTable(
                name: "RTOMWeights");

            migrationBuilder.DropTable(
                name: "SurveyTaskActivities");

            migrationBuilder.DropTable(
                name: "UDNames");

            migrationBuilder.DropTable(
                name: "UDSubCategories");

            migrationBuilder.DropTable(
                name: "UDCategories");

            migrationBuilder.DropColumn(
                name: "UrgentRequestedById",
                table: "PlannedEvents");

            migrationBuilder.DropColumn(
                name: "UrgentRequestedByName",
                table: "PlannedEvents");
        }
    }
}
