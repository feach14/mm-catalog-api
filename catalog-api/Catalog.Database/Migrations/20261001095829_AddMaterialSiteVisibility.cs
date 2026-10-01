using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialSiteVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "hide_on_site",
                schema: "catalog",
                table: "materials",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "hide_price_on_site",
                schema: "catalog",
                table: "materials",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "hide_on_site",
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
                name: "hide_on_site",
                schema: "catalog",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "hide_price_on_site",
                schema: "catalog",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "hide_on_site",
                schema: "catalog",
                table: "material_categories");
        }
    }
}