using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class RequireMaterialThickness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "depth",
                schema: "catalog",
                table: "materials");

            migrationBuilder.AlterColumn<int>(
                name: "material_thickness_id",
                schema: "catalog",
                table: "materials",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "material_thickness_id",
                schema: "catalog",
                table: "materials",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<double>(
                name: "depth",
                schema: "catalog",
                table: "materials",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.Sql("""
                UPDATE catalog.materials AS material
                SET depth = thickness.value
                FROM catalog.material_thicknesses AS thickness
                WHERE material.material_thickness_id = thickness.id;
                """);
        }
    }
}