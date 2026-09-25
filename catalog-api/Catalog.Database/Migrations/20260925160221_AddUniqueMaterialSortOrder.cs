using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueMaterialSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_materials_order_by_col",
                schema: "catalog",
                table: "materials");

            migrationBuilder.Sql("""
                WITH ordered_materials AS (
                    SELECT id, ROW_NUMBER() OVER (ORDER BY order_by_col, id) AS new_order_by_col
                    FROM catalog.materials
                )
                UPDATE catalog.materials AS material
                SET order_by_col = ordered_materials.new_order_by_col
                FROM ordered_materials
                WHERE material.id = ordered_materials.id;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_materials_order_by_col",
                schema: "catalog",
                table: "materials",
                column: "order_by_col",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_materials_order_by_col",
                schema: "catalog",
                table: "materials");

            migrationBuilder.CreateIndex(
                name: "ix_materials_order_by_col",
                schema: "catalog",
                table: "materials",
                column: "order_by_col");
        }
    }
}
