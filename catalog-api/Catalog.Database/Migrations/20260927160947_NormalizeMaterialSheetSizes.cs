using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeMaterialSheetSizes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "material_sheet_sizes",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    height = table.Column<int>(type: "integer", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    order_by_col = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_material_sheet_sizes", x => x.id);
                });

            migrationBuilder.AddColumn<int>(
                name: "material_sheet_size_id",
                schema: "catalog",
                table: "materials",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    unsupported_sizes text;
                BEGIN
                    SELECT string_agg(DISTINCT quote_literal(size), ', ' ORDER BY quote_literal(size))
                    INTO unsupported_sizes
                    FROM catalog.materials
                    WHERE btrim(size) NOT IN ('27501830', '16', '1')
                      AND btrim(size) !~ '^([0-9]+)\s*[xх×]\s*([0-9]+)$';

                    IF unsupported_sizes IS NOT NULL THEN
                        RAISE EXCEPTION
                            'Не удалось перенести размеры материалов. Неподдерживаемые значения: %',
                            unsupported_sizes;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql(
                """
                WITH parsed AS (
                    SELECT
                        id,
                        CASE
                            WHEN btrim(size) IN ('27501830', '16') THEN 2750
                            WHEN btrim(size) = '1' THEN 1
                            ELSE ((regexp_match(btrim(size), '^([0-9]+)\s*[xх×]\s*([0-9]+)$'))[1])::integer
                        END AS height,
                        CASE
                            WHEN btrim(size) IN ('27501830', '16') THEN 1830
                            WHEN btrim(size) = '1' THEN 1
                            ELSE ((regexp_match(btrim(size), '^([0-9]+)\s*[xх×]\s*([0-9]+)$'))[2])::integer
                        END AS width
                    FROM catalog.materials
                )
                , distinct_sizes AS (
                    SELECT DISTINCT height, width
                    FROM parsed
                    WHERE height IS NOT NULL AND width IS NOT NULL
                )
                INSERT INTO catalog.material_sheet_sizes (height, width, order_by_col)
                SELECT height, width, row_number() OVER (ORDER BY height, width)::integer
                FROM distinct_sizes;

                WITH parsed AS (
                    SELECT
                        id,
                        CASE
                            WHEN btrim(size) IN ('27501830', '16') THEN 2750
                            WHEN btrim(size) = '1' THEN 1
                            ELSE ((regexp_match(btrim(size), '^([0-9]+)\s*[xх×]\s*([0-9]+)$'))[1])::integer
                        END AS height,
                        CASE
                            WHEN btrim(size) IN ('27501830', '16') THEN 1830
                            WHEN btrim(size) = '1' THEN 1
                            ELSE ((regexp_match(btrim(size), '^([0-9]+)\s*[xх×]\s*([0-9]+)$'))[2])::integer
                        END AS width
                    FROM catalog.materials
                )
                UPDATE catalog.materials AS material
                SET material_sheet_size_id = material_sheet_size.id
                FROM parsed
                JOIN catalog.material_sheet_sizes AS material_sheet_size
                  ON material_sheet_size.height = parsed.height AND material_sheet_size.width = parsed.width
                WHERE material.id = parsed.id;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "material_sheet_size_id",
                schema: "catalog",
                table: "materials",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "size",
                schema: "catalog",
                table: "materials");

            migrationBuilder.CreateIndex(
                name: "ix_materials_material_sheet_size_id",
                schema: "catalog",
                table: "materials",
                column: "material_sheet_size_id");

            migrationBuilder.CreateIndex(
                name: "ix_material_sheet_sizes_height_width",
                schema: "catalog",
                table: "material_sheet_sizes",
                columns: new[] { "height", "width" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_material_sheet_sizes_order_by_col",
                schema: "catalog",
                table: "material_sheet_sizes",
                column: "order_by_col",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_materials_material_sheet_sizes_material_sheet_size_id",
                schema: "catalog",
                table: "materials",
                column: "material_sheet_size_id",
                principalSchema: "catalog",
                principalTable: "material_sheet_sizes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "size",
                schema: "catalog",
                table: "materials",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE catalog.materials AS material
                SET size = material_sheet_size.height::text || 'x' || material_sheet_size.width::text
                FROM catalog.material_sheet_sizes AS material_sheet_size
                WHERE material.material_sheet_size_id = material_sheet_size.id;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "size",
                schema: "catalog",
                table: "materials",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.DropForeignKey(
                name: "fk_materials_material_sheet_sizes_material_sheet_size_id",
                schema: "catalog",
                table: "materials");

            migrationBuilder.DropTable(
                name: "material_sheet_sizes",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "ix_materials_material_sheet_size_id",
                schema: "catalog",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "material_sheet_size_id",
                schema: "catalog",
                table: "materials");

        }
    }
}