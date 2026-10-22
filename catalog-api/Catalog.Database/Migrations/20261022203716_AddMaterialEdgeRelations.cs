using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialEdgeRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "material_edge_links",
                schema: "catalog",
                columns: table => new
                {
                    material_id = table.Column<int>(type: "integer", nullable: false),
                    edge_id = table.Column<int>(type: "integer", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_material_edge_links", x => new { x.material_id, x.edge_id });
                    table.ForeignKey(
                        name: "fk_material_edge_links_edges_edge_id",
                        column: x => x.edge_id,
                        principalSchema: "catalog",
                        principalTable: "edges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_material_edge_links_materials_material_id",
                        column: x => x.material_id,
                        principalSchema: "catalog",
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "relation_markings",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, computedColumnSql: "lower(btrim(name))", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_relation_markings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "material_edge_link_markings",
                schema: "catalog",
                columns: table => new
                {
                    material_id = table.Column<int>(type: "integer", nullable: false),
                    edge_id = table.Column<int>(type: "integer", nullable: false),
                    marking_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_material_edge_link_markings", x => new { x.material_id, x.edge_id, x.marking_id });
                    table.ForeignKey(
                        name: "fk_material_edge_link_markings_material_edge_links_material_id",
                        columns: x => new { x.material_id, x.edge_id },
                        principalSchema: "catalog",
                        principalTable: "material_edge_links",
                        principalColumns: new[] { "material_id", "edge_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_material_edge_link_markings_relation_markings_marking_id",
                        column: x => x.marking_id,
                        principalSchema: "catalog",
                        principalTable: "relation_markings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_material_edge_link_markings_marking_id",
                schema: "catalog",
                table: "material_edge_link_markings",
                column: "marking_id");

            migrationBuilder.CreateIndex(
                name: "ix_material_edge_links_edge_id",
                schema: "catalog",
                table: "material_edge_links",
                column: "edge_id");

            migrationBuilder.CreateIndex(
                name: "ix_material_edge_links_material_id_priority",
                schema: "catalog",
                table: "material_edge_links",
                columns: new[] { "material_id", "priority" });

            migrationBuilder.CreateIndex(
                name: "ix_relation_markings_normalized_name",
                schema: "catalog",
                table: "relation_markings",
                column: "normalized_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "material_edge_link_markings",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "material_edge_links",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "relation_markings",
                schema: "catalog");
        }
    }
}