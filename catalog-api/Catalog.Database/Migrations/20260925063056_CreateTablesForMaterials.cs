using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class CreateTablesForMaterials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.CreateTable(
                name: "material_categories",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    order_by_col = table.Column<int>(type: "integer", nullable: false),
                    external_link = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_material_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "materials",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    category_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    article = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    depth = table.Column<double>(type: "double precision", nullable: false),
                    kv_m = table.Column<double>(type: "double precision", nullable: false),
                    perimetr_m = table.Column<double>(type: "double precision", nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false),
                    external_link = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    deleted = table.Column<bool>(type: "boolean", nullable: false),
                    comment_on_material_is_required = table.Column<bool>(type: "boolean", nullable: false),
                    allow_second_item_in_order = table.Column<bool>(type: "boolean", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    count_type_enum = table.Column<int>(type: "integer", nullable: false),
                    order_by_col = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    default_pvh_facade = table.Column<bool>(type: "boolean", nullable: false),
                    default_emal_facade = table.Column<bool>(type: "boolean", nullable: false),
                    applicable_to_raskroys = table.Column<bool>(type: "boolean", nullable: false),
                    applicable_to_pvh_facades = table.Column<bool>(type: "boolean", nullable: false),
                    applicable_to_emal_facades = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_materials", x => x.id);
                    table.ForeignKey(
                        name: "fk_materials_material_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "catalog",
                        principalTable: "material_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "material_images",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    material_id = table.Column<int>(type: "integer", nullable: false),
                    guid = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    data = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_material_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_material_images_materials_material_id",
                        column: x => x.material_id,
                        principalSchema: "catalog",
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_material_categories_order_by_col",
                schema: "catalog",
                table: "material_categories",
                column: "order_by_col",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_material_images_material_id",
                schema: "catalog",
                table: "material_images",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "ix_materials_category_id",
                schema: "catalog",
                table: "materials",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_materials_order_by_col",
                schema: "catalog",
                table: "materials",
                column: "order_by_col");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "material_images",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "materials",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "material_categories",
                schema: "catalog");
        }
    }
}
