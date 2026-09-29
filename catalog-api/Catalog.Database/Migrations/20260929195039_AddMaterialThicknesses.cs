using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialThicknesses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "material_thickness_id",
                schema: "catalog",
                table: "materials",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "material_thicknesses",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_material_thicknesses", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_materials_material_thickness_id",
                schema: "catalog",
                table: "materials",
                column: "material_thickness_id");

            migrationBuilder.CreateIndex(
                name: "ix_material_thicknesses_value",
                schema: "catalog",
                table: "material_thicknesses",
                column: "value",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_materials_material_thicknesses_material_thickness_id",
                schema: "catalog",
                table: "materials",
                column: "material_thickness_id",
                principalSchema: "catalog",
                principalTable: "material_thicknesses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                INSERT INTO catalog.material_thicknesses (name, value)
                SELECT DISTINCT replace(depth::text, '.', ',') || ' мм', depth
                FROM catalog.materials
                ORDER BY depth;

                UPDATE catalog.materials AS material
                SET material_thickness_id = thickness.id
                FROM catalog.material_thicknesses AS thickness
                WHERE material.depth = thickness.value;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_materials_material_thicknesses_material_thickness_id",
                schema: "catalog",
                table: "materials");

            migrationBuilder.DropTable(
                name: "material_thicknesses",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "ix_materials_material_thickness_id",
                schema: "catalog",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "material_thickness_id",
                schema: "catalog",
                table: "materials");
        }
    }
}