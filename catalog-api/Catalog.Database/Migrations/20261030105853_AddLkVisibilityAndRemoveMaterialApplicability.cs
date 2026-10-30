using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddLkVisibilityAndRemoveMaterialApplicability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "applicable_to_cutting",
                schema: "catalog",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "applicable_to_enamel_facades",
                schema: "catalog",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "applicable_to_pvh_facades",
                schema: "catalog",
                table: "materials");

            migrationBuilder.AddColumn<bool>(
                name: "hide_in_lk",
                schema: "catalog",
                table: "materials",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "hide_in_lk",
                schema: "catalog",
                table: "material_categories",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "hide_in_lk",
                schema: "catalog",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "hide_in_lk",
                schema: "catalog",
                table: "material_categories");

            migrationBuilder.AddColumn<bool>(
                name: "applicable_to_cutting",
                schema: "catalog",
                table: "materials",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "applicable_to_enamel_facades",
                schema: "catalog",
                table: "materials",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "applicable_to_pvh_facades",
                schema: "catalog",
                table: "materials",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}