using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SFCDashboard.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCascadeDeleteBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BOQs_UDCategories_CategoryId",
                table: "BOQs");

            migrationBuilder.DropForeignKey(
                name: "FK_BOQs_UDNames_UDNameId",
                table: "BOQs");

            migrationBuilder.DropForeignKey(
                name: "FK_BOQs_UDSubCategories_SubCategoryId",
                table: "BOQs");

            migrationBuilder.DropForeignKey(
                name: "FK_BOQs_Users_CreatedByUserId",
                table: "BOQs");

            migrationBuilder.DropForeignKey(
                name: "FK_UDNames_UDCategories_CategoryId",
                table: "UDNames");

            migrationBuilder.AddForeignKey(
                name: "FK_BOQs_UDCategories_CategoryId",
                table: "BOQs",
                column: "CategoryId",
                principalTable: "UDCategories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BOQs_UDNames_UDNameId",
                table: "BOQs",
                column: "UDNameId",
                principalTable: "UDNames",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BOQs_UDSubCategories_SubCategoryId",
                table: "BOQs",
                column: "SubCategoryId",
                principalTable: "UDSubCategories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BOQs_Users_CreatedByUserId",
                table: "BOQs",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UDNames_UDCategories_CategoryId",
                table: "UDNames",
                column: "CategoryId",
                principalTable: "UDCategories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BOQs_UDCategories_CategoryId",
                table: "BOQs");

            migrationBuilder.DropForeignKey(
                name: "FK_BOQs_UDNames_UDNameId",
                table: "BOQs");

            migrationBuilder.DropForeignKey(
                name: "FK_BOQs_UDSubCategories_SubCategoryId",
                table: "BOQs");

            migrationBuilder.DropForeignKey(
                name: "FK_BOQs_Users_CreatedByUserId",
                table: "BOQs");

            migrationBuilder.DropForeignKey(
                name: "FK_UDNames_UDCategories_CategoryId",
                table: "UDNames");

            migrationBuilder.AddForeignKey(
                name: "FK_BOQs_UDCategories_CategoryId",
                table: "BOQs",
                column: "CategoryId",
                principalTable: "UDCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BOQs_UDNames_UDNameId",
                table: "BOQs",
                column: "UDNameId",
                principalTable: "UDNames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BOQs_UDSubCategories_SubCategoryId",
                table: "BOQs",
                column: "SubCategoryId",
                principalTable: "UDSubCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BOQs_Users_CreatedByUserId",
                table: "BOQs",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UDNames_UDCategories_CategoryId",
                table: "UDNames",
                column: "CategoryId",
                principalTable: "UDCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
