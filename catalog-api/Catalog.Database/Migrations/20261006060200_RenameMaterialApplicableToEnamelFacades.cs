using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameMaterialApplicableToEnamelFacades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "applicable_to_emal_facades",
                schema: "catalog",
                table: "materials",
                newName: "applicable_to_enamel_facades");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "applicable_to_enamel_facades",
                schema: "catalog",
                table: "materials",
                newName: "applicable_to_emal_facades");
        }
    }
}