using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameMaterialApplicableToCutting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "applicable_to_raskroys",
                schema: "catalog",
                table: "materials",
                newName: "applicable_to_cutting");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "applicable_to_cutting",
                schema: "catalog",
                table: "materials",
                newName: "applicable_to_raskroys");
        }
    }
}