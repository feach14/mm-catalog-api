using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddEdgeSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "order_by_col",
                schema: "catalog",
                table: "edges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_edges_order_by_col_id",
                schema: "catalog",
                table: "edges",
                columns: new[] { "order_by_col", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_edges_order_by_col_id",
                schema: "catalog",
                table: "edges");

            migrationBuilder.DropColumn(
                name: "order_by_col",
                schema: "catalog",
                table: "edges");
        }
    }
}