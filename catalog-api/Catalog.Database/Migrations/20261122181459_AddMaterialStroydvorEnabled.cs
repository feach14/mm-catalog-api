using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialStroydvorEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "stroydvor_enabled",
                schema: "catalog",
                table: "materials",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "stroydvor_enabled",
                schema: "catalog",
                table: "archive_materials",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "stroydvor_enabled",
                schema: "catalog",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "stroydvor_enabled",
                schema: "catalog",
                table: "archive_materials");
        }
    }
}