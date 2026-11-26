using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddEdgeSuppliers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "supplier_id",
                schema: "catalog",
                table: "edges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "edge_suppliers",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    include_in_edge_name = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    order_by_col = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    normalized_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, computedColumnSql: "lower(btrim(name))", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_edge_suppliers", x => x.id);
                });

            migrationBuilder.Sql("""
                WITH supplier AS (
                    INSERT INTO catalog.edge_suppliers (name, include_in_edge_name, order_by_col)
                    VALUES ('Стройдвор', false, 1)
                    RETURNING id
                )
                UPDATE catalog.edges
                SET supplier_id = (SELECT id FROM supplier);

                ALTER TABLE catalog.edges ALTER COLUMN supplier_id DROP DEFAULT;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_edges_supplier_id",
                schema: "catalog",
                table: "edges",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_edge_suppliers_normalized_name",
                schema: "catalog",
                table: "edge_suppliers",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_edge_suppliers_order_by_col_id",
                schema: "catalog",
                table: "edge_suppliers",
                columns: new[] { "order_by_col", "id" });

            migrationBuilder.AddForeignKey(
                name: "fk_edges_edge_suppliers_supplier_id",
                schema: "catalog",
                table: "edges",
                column: "supplier_id",
                principalSchema: "catalog",
                principalTable: "edge_suppliers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_edges_edge_suppliers_supplier_id",
                schema: "catalog",
                table: "edges");

            migrationBuilder.DropTable(
                name: "edge_suppliers",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "ix_edges_supplier_id",
                schema: "catalog",
                table: "edges");

            migrationBuilder.DropColumn(
                name: "supplier_id",
                schema: "catalog",
                table: "edges");
        }
    }
}