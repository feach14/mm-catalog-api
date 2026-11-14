using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameEdgeThicknessToEdgeSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_edges_edge_thicknesses_thickness_id",
                schema: "catalog",
                table: "edges");

            migrationBuilder.DropPrimaryKey(
                name: "pk_edge_thicknesses",
                schema: "catalog",
                table: "edge_thicknesses");

            migrationBuilder.DropIndex(
                name: "ix_edge_thicknesses_normalized_name",
                schema: "catalog",
                table: "edge_thicknesses");

            migrationBuilder.DropColumn(
                name: "normalized_name",
                schema: "catalog",
                table: "edge_thicknesses");

            migrationBuilder.RenameTable(
                name: "edge_thicknesses",
                schema: "catalog",
                newName: "edge_sizes",
                newSchema: "catalog");

            migrationBuilder.RenameColumn(
                name: "thickness_id",
                schema: "catalog",
                table: "edges",
                newName: "size_id");

            migrationBuilder.RenameIndex(
                name: "ix_edges_thickness_id",
                schema: "catalog",
                table: "edges",
                newName: "ix_edges_size_id");

            migrationBuilder.RenameColumn(
                name: "value",
                schema: "catalog",
                table: "edge_sizes",
                newName: "thickness");

            migrationBuilder.RenameIndex(
                name: "ix_edge_thicknesses_value_width",
                schema: "catalog",
                table: "edge_sizes",
                newName: "ix_edge_sizes_thickness_width");

            migrationBuilder.RenameIndex(
                name: "ix_edge_thicknesses_order_by_col_id",
                schema: "catalog",
                table: "edge_sizes",
                newName: "ix_edge_sizes_order_by_col_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_edge_sizes",
                schema: "catalog",
                table: "edge_sizes",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_edges_edge_sizes_size_id",
                schema: "catalog",
                table: "edges",
                column: "size_id",
                principalSchema: "catalog",
                principalTable: "edge_sizes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_edges_edge_sizes_size_id",
                schema: "catalog",
                table: "edges");

            migrationBuilder.DropPrimaryKey(
                name: "pk_edge_sizes",
                schema: "catalog",
                table: "edge_sizes");

            migrationBuilder.RenameTable(
                name: "edge_sizes",
                schema: "catalog",
                newName: "edge_thicknesses",
                newSchema: "catalog");

            migrationBuilder.RenameColumn(
                name: "size_id",
                schema: "catalog",
                table: "edges",
                newName: "thickness_id");

            migrationBuilder.RenameIndex(
                name: "ix_edges_size_id",
                schema: "catalog",
                table: "edges",
                newName: "ix_edges_thickness_id");

            migrationBuilder.RenameColumn(
                name: "thickness",
                schema: "catalog",
                table: "edge_thicknesses",
                newName: "value");

            migrationBuilder.RenameIndex(
                name: "ix_edge_sizes_thickness_width",
                schema: "catalog",
                table: "edge_thicknesses",
                newName: "ix_edge_thicknesses_value_width");

            migrationBuilder.RenameIndex(
                name: "ix_edge_sizes_order_by_col_id",
                schema: "catalog",
                table: "edge_thicknesses",
                newName: "ix_edge_thicknesses_order_by_col_id");

            migrationBuilder.AddColumn<string>(
                name: "normalized_name",
                schema: "catalog",
                table: "edge_thicknesses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                computedColumnSql: "lower(btrim(name))",
                stored: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_edge_thicknesses",
                schema: "catalog",
                table: "edge_thicknesses",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_edge_thicknesses_normalized_name",
                schema: "catalog",
                table: "edge_thicknesses",
                column: "normalized_name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_edges_edge_thicknesses_thickness_id",
                schema: "catalog",
                table: "edges",
                column: "thickness_id",
                principalSchema: "catalog",
                principalTable: "edge_thicknesses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}