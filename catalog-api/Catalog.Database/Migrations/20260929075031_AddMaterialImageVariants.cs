using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialImageVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_material_images_material_id",
                schema: "catalog",
                table: "material_images");

            migrationBuilder.AddColumn<string>(
                name: "image_type",
                schema: "catalog",
                table: "material_images",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Original");

            migrationBuilder.AddColumn<string>(
                name: "image_type",
                schema: "catalog",
                table: "image_cache",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Original");

            migrationBuilder.CreateIndex(
                name: "ix_material_images_image_type",
                schema: "catalog",
                table: "material_images",
                column: "image_type");

            migrationBuilder.CreateIndex(
                name: "ix_material_images_material_id_image_type",
                schema: "catalog",
                table: "material_images",
                columns: new[] { "material_id", "image_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_image_cache_image_type",
                schema: "catalog",
                table: "image_cache",
                column: "image_type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_material_images_image_type",
                schema: "catalog",
                table: "material_images");

            migrationBuilder.DropIndex(
                name: "ix_material_images_material_id_image_type",
                schema: "catalog",
                table: "material_images");

            migrationBuilder.DropIndex(
                name: "ix_image_cache_image_type",
                schema: "catalog",
                table: "image_cache");

            migrationBuilder.DropColumn(
                name: "image_type",
                schema: "catalog",
                table: "material_images");

            migrationBuilder.DropColumn(
                name: "image_type",
                schema: "catalog",
                table: "image_cache");

            migrationBuilder.CreateIndex(
                name: "ix_material_images_material_id",
                schema: "catalog",
                table: "material_images",
                column: "material_id");
        }
    }
}
