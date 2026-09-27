using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class SeedManufacturersAndRequireManufacturer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                WITH source(name, sort_order) AS (
                    VALUES
                        ('Увадрев', 1),
                        ('Swiss Krono', 2),
                        ('Ultradecor', 3),
                        ('Egger', 4),
                        ('Lamarty', 5),
                        ('AGT', 6),
                        ('Evogloss', 7),
                        ('Nordeco', 8),
                        ('Byspan', 9),
                        ('Прочее', 10)
                ), current_order AS (
                    SELECT COALESCE(MAX(order_by_col), 0) AS value
                    FROM catalog.manufacturers
                )
                INSERT INTO catalog.manufacturers (name, order_by_col)
                SELECT source.name, current_order.value + source.sort_order
                FROM source
                CROSS JOIN current_order
                WHERE NOT EXISTS (
                    SELECT 1 FROM catalog.manufacturers existing WHERE existing.name = source.name
                );

                WITH detected AS (
                    SELECT material.id,
                    CASE
                        WHEN category.name ILIKE '%AGT%'
                             AND btrim(material.name) ~* '^(P|Р|EVA|EVS)'
                            THEN 'Evogloss'
                        WHEN material.name ILIKE ANY (ARRAY['%Увадрев%', '%UVADREV%'])
                            THEN 'Увадрев'
                        WHEN material.name ILIKE ANY (ARRAY['%Swiss%', '%Crono%', '%Свисс%', '%Кроно%'])
                            THEN 'Swiss Krono'
                        WHEN material.name ILIKE ANY (ARRAY['%Ультрадекор%', '%Ultradecor%'])
                            THEN 'Ultradecor'
                        WHEN material.name ILIKE ANY (ARRAY['%Egger%', '%Эггер%'])
                            THEN 'Egger'
                        WHEN material.name ILIKE ANY (ARRAY['%Lamarty%', '%Lamart%', '%Ламарти%', '%Ламарт%'])
                            THEN 'Lamarty'
                        WHEN material.name ILIKE ANY (ARRAY['%Nordeco%', '%Нордеко%'])
                            THEN 'Nordeco'
                        WHEN material.name ILIKE '%Byspan%'
                            THEN 'Byspan'
                        WHEN category.name ILIKE '%УВАДРЕВ%'
                             AND category.name NOT ILIKE '%Swiss%'
                            THEN 'Увадрев'
                        WHEN category.name ILIKE '%SWISS CRONO%'
                             AND category.name NOT ILIKE '%УваДрев%'
                            THEN 'Swiss Krono'
                        WHEN category.name ILIKE '%УЛЬТРАДЕКОР%'
                            THEN 'Ultradecor'
                        WHEN category.name ILIKE '%EGGER%'
                             AND category.name NOT ILIKE '%УваДрев%'
                            THEN 'Egger'
                        WHEN category.name ILIKE '%LAMARTY%'
                             AND category.name NOT ILIKE '%УваДрев%'
                            THEN 'Lamarty'
                        WHEN category.name ILIKE '%AGT%'
                            THEN 'AGT'
                        WHEN category.name ILIKE '%NORDECO%'
                             AND category.name NOT ILIKE '%УваДрев%'
                            THEN 'Nordeco'
                        WHEN category.name ILIKE '%BYSPAN%'
                            THEN 'Byspan'
                        ELSE 'Прочее'
                    END AS manufacturer_name
                    FROM catalog.materials material
                    JOIN catalog.material_categories category ON category.id = material.category_id
                )
                UPDATE catalog.materials material
                SET manufacturer_id = manufacturer.id
                FROM detected
                JOIN catalog.manufacturers manufacturer ON manufacturer.name = detected.manufacturer_name
                WHERE material.id = detected.id;

                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM catalog.materials WHERE manufacturer_id IS NULL) THEN
                        RAISE EXCEPTION 'Не всем материалам удалось назначить производителя';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "manufacturer_id",
                schema: "catalog",
                table: "materials",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "manufacturer_id",
                schema: "catalog",
                table: "materials",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.Sql(
                """
                UPDATE catalog.materials
                SET manufacturer_id = NULL
                WHERE manufacturer_id IN (
                    SELECT id
                    FROM catalog.manufacturers
                    WHERE name IN ('Увадрев', 'Swiss Krono', 'Ultradecor', 'Egger', 'Lamarty', 'AGT', 'Evogloss', 'Nordeco', 'Byspan', 'Прочее')
                );

                DELETE FROM catalog.manufacturers
                WHERE name IN ('Увадрев', 'Swiss Krono', 'Ultradecor', 'Egger', 'Lamarty', 'AGT', 'Evogloss', 'Nordeco', 'Byspan', 'Прочее');
                """);
        }
    }
}
