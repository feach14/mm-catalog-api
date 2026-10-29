using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameCatalogTypeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "type",
                schema: "catalog",
                table: "material_images",
                newName: "content_type");

            migrationBuilder.RenameColumn(
                name: "type",
                schema: "catalog",
                table: "image_cache",
                newName: "content_type");

            migrationBuilder.RenameColumn(
                name: "type",
                schema: "catalog",
                table: "edge_sale_units",
                newName: "sale_unit_type");

            migrationBuilder.RenameIndex(
                name: "ix_edge_sale_units_type_length",
                schema: "catalog",
                table: "edge_sale_units",
                newName: "ix_edge_sale_units_sale_unit_type_length");

            migrationBuilder.RenameColumn(
                name: "type",
                schema: "catalog",
                table: "edge_images",
                newName: "content_type");

            migrationBuilder.RenameColumn(
                name: "type",
                schema: "catalog",
                table: "edge_image_cache",
                newName: "content_type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "content_type",
                schema: "catalog",
                table: "material_images",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "content_type",
                schema: "catalog",
                table: "image_cache",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "sale_unit_type",
                schema: "catalog",
                table: "edge_sale_units",
                newName: "type");

            migrationBuilder.RenameIndex(
                name: "ix_edge_sale_units_sale_unit_type_length",
                schema: "catalog",
                table: "edge_sale_units",
                newName: "ix_edge_sale_units_type_length");

            migrationBuilder.RenameColumn(
                name: "content_type",
                schema: "catalog",
                table: "edge_images",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "content_type",
                schema: "catalog",
                table: "edge_image_cache",
                newName: "type");
        }
    }
}