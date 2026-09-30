using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class RequireMaterialType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "material_type_id",
                schema: "catalog",
                table: "materials",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                DO $$
                DECLARE ldsp_id integer;
                BEGIN
                    IF (SELECT COUNT(*) FROM catalog.material_types WHERE name = 'LDSP') <> 1 THEN
                        RAISE EXCEPTION 'Для привязки материалов требуется ровно один тип LDSP. Создайте его через API перед применением миграции.';
                    END IF;
                    SELECT id INTO ldsp_id FROM catalog.material_types WHERE name = 'LDSP';
                    UPDATE catalog.materials SET material_type_id = ldsp_id;
                END $$;
                ALTER TABLE catalog.materials ALTER COLUMN material_type_id DROP DEFAULT;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_materials_material_type_id",
                schema: "catalog",
                table: "materials",
                column: "material_type_id");

            migrationBuilder.AddForeignKey(
                name: "fk_materials_material_types_material_type_id",
                schema: "catalog",
                table: "materials",
                column: "material_type_id",
                principalSchema: "catalog",
                principalTable: "material_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_materials_material_types_material_type_id",
                schema: "catalog",
                table: "materials");

            migrationBuilder.DropIndex(
                name: "ix_materials_material_type_id",
                schema: "catalog",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "material_type_id",
                schema: "catalog",
                table: "materials");
        }
    }
}