using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialManagerComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "material_comments",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    material_id = table.Column<int>(type: "integer", nullable: true),
                    archived_material_id = table.Column<int>(type: "integer", nullable: true),
                    text = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_material_comments", x => x.id);
                    table.ForeignKey(
                        name: "fk_material_comments_archived_materials_archived_material_id",
                        column: x => x.archived_material_id,
                        principalSchema: "catalog",
                        principalTable: "archive_materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_material_comments_materials_material_id",
                        column: x => x.material_id,
                        principalSchema: "catalog",
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_material_comments_archived_material_id_id",
                schema: "catalog",
                table: "material_comments",
                columns: new[] { "archived_material_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_material_comments_material_id_id",
                schema: "catalog",
                table: "material_comments",
                columns: new[] { "material_id", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "material_comments",
                schema: "catalog");
        }
    }
}