using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMaterialSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM catalog.material_images
                WHERE material_id IN (SELECT id FROM catalog.materials WHERE deleted = TRUE);

                DELETE FROM catalog.materials
                WHERE deleted = TRUE;
                """);

            migrationBuilder.DropColumn(
                name: "deleted",
                schema: "catalog",
                table: "materials");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "deleted",
                schema: "catalog",
                table: "materials",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}