using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddEdgeArticleReplacements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "edge_article_replacements",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    edge_id = table.Column<int>(type: "integer", nullable: false),
                    old_article = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_edge_article_replacements", x => x.id);
                    table.ForeignKey(
                        name: "fk_edge_article_replacements_edges_edge_id",
                        column: x => x.edge_id,
                        principalSchema: "catalog",
                        principalTable: "edges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_edge_article_replacements_edge_id",
                schema: "catalog",
                table: "edge_article_replacements",
                column: "edge_id");

            migrationBuilder.CreateIndex(
                name: "ix_edge_article_replacements_old_article",
                schema: "catalog",
                table: "edge_article_replacements",
                column: "old_article",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "edge_article_replacements",
                schema: "catalog");
        }
    }
}