using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameManufacturersToMaterialManufacturers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_materials_manufacturers_manufacturer_id",
                schema: "catalog",
                table: "materials");

            migrationBuilder.RenameTable(
                name: "manufacturers",
                schema: "catalog",
                newName: "material_manufacturers",
                newSchema: "catalog");

            migrationBuilder.RenameColumn(
                name: "manufacturer_id",
                schema: "catalog",
                table: "materials",
                newName: "material_manufacturer_id");

            migrationBuilder.RenameIndex(
                name: "ix_materials_manufacturer_id",
                schema: "catalog",
                table: "materials",
                newName: "ix_materials_material_manufacturer_id");

            migrationBuilder.RenameIndex(
                name: "ix_manufacturers_order_by_col",
                schema: "catalog",
                table: "material_manufacturers",
                newName: "ix_material_manufacturers_order_by_col");

            migrationBuilder.Sql(
                "ALTER TABLE catalog.material_manufacturers RENAME CONSTRAINT pk_manufacturers TO pk_material_manufacturers;");

            migrationBuilder.AddForeignKey(
                name: "fk_materials_material_manufacturers_material_manufacturer_id",
                schema: "catalog",
                table: "materials",
                column: "material_manufacturer_id",
                principalSchema: "catalog",
                principalTable: "material_manufacturers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_materials_material_manufacturers_material_manufacturer_id",
                schema: "catalog",
                table: "materials");

            migrationBuilder.Sql(
                "ALTER TABLE catalog.material_manufacturers RENAME CONSTRAINT pk_material_manufacturers TO pk_manufacturers;");

            migrationBuilder.RenameIndex(
                name: "ix_material_manufacturers_order_by_col",
                schema: "catalog",
                table: "material_manufacturers",
                newName: "ix_manufacturers_order_by_col");

            migrationBuilder.RenameColumn(
                name: "material_manufacturer_id",
                schema: "catalog",
                table: "materials",
                newName: "manufacturer_id");

            migrationBuilder.RenameIndex(
                name: "ix_materials_material_manufacturer_id",
                schema: "catalog",
                table: "materials",
                newName: "ix_materials_manufacturer_id");

            migrationBuilder.RenameTable(
                name: "material_manufacturers",
                schema: "catalog",
                newName: "manufacturers",
                newSchema: "catalog");

            migrationBuilder.AddForeignKey(
                name: "fk_materials_manufacturers_manufacturer_id",
                schema: "catalog",
                table: "materials",
                column: "manufacturer_id",
                principalSchema: "catalog",
                principalTable: "manufacturers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}