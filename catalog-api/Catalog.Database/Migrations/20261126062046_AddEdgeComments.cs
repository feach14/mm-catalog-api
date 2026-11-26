using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddEdgeComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "edge_comments",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    edge_id = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_for_client = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_edge_comments", x => x.id);
                    table.ForeignKey(
                        name: "fk_edge_comments_edges_edge_id",
                        column: x => x.edge_id,
                        principalSchema: "catalog",
                        principalTable: "edges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_edge_comments_edge_id_id",
                schema: "catalog",
                table: "edge_comments",
                columns: new[] { "edge_id", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "edge_comments",
                schema: "catalog");
        }
    }
}