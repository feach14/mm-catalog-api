using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddEdgesCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "edge_image_cache",
                schema: "catalog",
                columns: table => new
                {
                    guid = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    data = table.Column<byte[]>(type: "bytea", nullable: false),
                    image_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_edge_image_cache", x => x.guid);
                });

            migrationBuilder.CreateTable(
                name: "edge_manufacturers",
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
                    table.PrimaryKey("pk_edge_manufacturers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "edge_sale_units",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    length = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, computedColumnSql: "lower(btrim(name))", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_edge_sale_units", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "edge_thicknesses",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, computedColumnSql: "lower(btrim(name))", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_edge_thicknesses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "edges",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    article = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    manufacturer_id = table.Column<int>(type: "integer", nullable: false),
                    thickness_id = table.Column<int>(type: "integer", nullable: false),
                    sale_unit_id = table.Column<int>(type: "integer", nullable: false),
                    is_stock_item = table.Column<bool>(type: "boolean", nullable: false),
                    stock_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_edges", x => x.id);
                    table.ForeignKey(
                        name: "fk_edges_edge_manufacturers_manufacturer_id",
                        column: x => x.manufacturer_id,
                        principalSchema: "catalog",
                        principalTable: "edge_manufacturers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_edges_edge_sale_units_sale_unit_id",
                        column: x => x.sale_unit_id,
                        principalSchema: "catalog",
                        principalTable: "edge_sale_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_edges_edge_thicknesses_thickness_id",
                        column: x => x.thickness_id,
                        principalSchema: "catalog",
                        principalTable: "edge_thicknesses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "edge_images",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    edge_id = table.Column<int>(type: "integer", nullable: false),
                    guid = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    data = table.Column<byte[]>(type: "bytea", nullable: false),
                    image_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_edge_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_edge_images_edges_edge_id",
                        column: x => x.edge_id,
                        principalSchema: "catalog",
                        principalTable: "edges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_edge_images_edge_id_image_type",
                schema: "catalog",
                table: "edge_images",
                columns: new[] { "edge_id", "image_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_edge_images_guid",
                schema: "catalog",
                table: "edge_images",
                column: "guid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_edge_manufacturers_normalized_name",
                schema: "catalog",
                table: "edge_manufacturers",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_edge_sale_units_normalized_name",
                schema: "catalog",
                table: "edge_sale_units",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_edge_sale_units_type_length",
                schema: "catalog",
                table: "edge_sale_units",
                columns: new[] { "type", "length" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_edge_thicknesses_normalized_name",
                schema: "catalog",
                table: "edge_thicknesses",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_edge_thicknesses_value",
                schema: "catalog",
                table: "edge_thicknesses",
                column: "value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_edges_manufacturer_id",
                schema: "catalog",
                table: "edges",
                column: "manufacturer_id");

            migrationBuilder.CreateIndex(
                name: "ix_edges_sale_unit_id",
                schema: "catalog",
                table: "edges",
                column: "sale_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_edges_thickness_id",
                schema: "catalog",
                table: "edges",
                column: "thickness_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "edge_image_cache",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "edge_images",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "edges",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "edge_manufacturers",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "edge_sale_units",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "edge_thicknesses",
                schema: "catalog");
        }
    }
}