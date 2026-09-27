using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDefaultMaterialFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "default_emal_facade",
                schema: "catalog",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "default_pvh_facade",
                schema: "catalog",
                table: "materials");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "default_emal_facade",
                schema: "catalog",
                table: "materials",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "default_pvh_facade",
                schema: "catalog",
                table: "materials",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}