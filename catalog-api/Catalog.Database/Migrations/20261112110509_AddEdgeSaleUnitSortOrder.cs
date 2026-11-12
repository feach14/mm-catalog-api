using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddEdgeSaleUnitSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "order_by_col",
                schema: "catalog",
                table: "edge_sale_units",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_edge_sale_units_order_by_col_name_id",
                schema: "catalog",
                table: "edge_sale_units",
                columns: new[] { "order_by_col", "name", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_edge_sale_units_order_by_col_name_id",
                schema: "catalog",
                table: "edge_sale_units");

            migrationBuilder.DropColumn(
                name: "order_by_col",
                schema: "catalog",
                table: "edge_sale_units");
        }
    }
}