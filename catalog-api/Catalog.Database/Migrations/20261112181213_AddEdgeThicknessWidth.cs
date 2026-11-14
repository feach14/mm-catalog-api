using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddEdgeThicknessWidth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_edge_thicknesses_value",
                schema: "catalog",
                table: "edge_thicknesses");

            migrationBuilder.AddColumn<decimal>(
                name: "width",
                schema: "catalog",
                table: "edge_thicknesses",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "ix_edge_thicknesses_value_width",
                schema: "catalog",
                table: "edge_thicknesses",
                columns: new[] { "value", "width" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_edge_thicknesses_value_width",
                schema: "catalog",
                table: "edge_thicknesses");

            migrationBuilder.DropColumn(
                name: "width",
                schema: "catalog",
                table: "edge_thicknesses");

            migrationBuilder.CreateIndex(
                name: "ix_edge_thicknesses_value",
                schema: "catalog",
                table: "edge_thicknesses",
                column: "value",
                unique: true);
        }
    }
}