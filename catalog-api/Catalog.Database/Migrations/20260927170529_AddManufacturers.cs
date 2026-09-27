using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddManufacturers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "manufacturer_id",
                schema: "catalog",
                table: "materials",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "manufacturers",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    order_by_col = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_manufacturers", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_materials_manufacturer_id",
                schema: "catalog",
                table: "materials",
                column: "manufacturer_id");

            migrationBuilder.CreateIndex(
                name: "ix_manufacturers_order_by_col",
                schema: "catalog",
                table: "manufacturers",
                column: "order_by_col",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_materials_manufacturers_manufacturer_id",
                schema: "catalog",
                table: "materials",
                column: "manufacturer_id",
                principalSchema: "catalog",
                principalTable: "manufacturers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_materials_manufacturers_manufacturer_id",
                schema: "catalog",
                table: "materials");

            migrationBuilder.DropTable(
                name: "manufacturers",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "ix_materials_manufacturer_id",
                schema: "catalog",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "manufacturer_id",
                schema: "catalog",
                table: "materials");
        }
    }
}