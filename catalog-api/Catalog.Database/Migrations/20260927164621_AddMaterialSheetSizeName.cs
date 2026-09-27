using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialSheetSizeName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "name",
                schema: "catalog",
                table: "material_sheet_sizes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE catalog.material_sheet_sizes
                SET name = height::text || '×' || width::text;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "catalog",
                table: "material_sheet_sizes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "name",
                schema: "catalog",
                table: "material_sheet_sizes");
        }
    }
}