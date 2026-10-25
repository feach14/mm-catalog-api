using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationMarkingSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "order_by_col",
                schema: "catalog",
                table: "relation_markings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_relation_markings_order_by_col_id",
                schema: "catalog",
                table: "relation_markings",
                columns: new[] { "order_by_col", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_relation_markings_order_by_col_id",
                schema: "catalog",
                table: "relation_markings");

            migrationBuilder.DropColumn(
                name: "order_by_col",
                schema: "catalog",
                table: "relation_markings");
        }
    }
}