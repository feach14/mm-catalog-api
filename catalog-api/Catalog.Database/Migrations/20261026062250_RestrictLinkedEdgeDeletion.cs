using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class RestrictLinkedEdgeDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_material_edge_links_edges_edge_id",
                schema: "catalog",
                table: "material_edge_links");

            migrationBuilder.AddForeignKey(
                name: "fk_material_edge_links_edges_edge_id",
                schema: "catalog",
                table: "material_edge_links",
                column: "edge_id",
                principalSchema: "catalog",
                principalTable: "edges",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_material_edge_links_edges_edge_id",
                schema: "catalog",
                table: "material_edge_links");

            migrationBuilder.AddForeignKey(
                name: "fk_material_edge_links_edges_edge_id",
                schema: "catalog",
                table: "material_edge_links",
                column: "edge_id",
                principalSchema: "catalog",
                principalTable: "edges",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}