using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogChangeHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "change_history",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    action_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    entity_id = table.Column<int>(type: "integer", nullable: false),
                    user_phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_change_history", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_change_history_action_type",
                schema: "catalog",
                table: "change_history",
                column: "action_type");

            migrationBuilder.CreateIndex(
                name: "ix_change_history_entity_type_entity_id",
                schema: "catalog",
                table: "change_history",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_change_history_occurred_at",
                schema: "catalog",
                table: "change_history",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_change_history_user_phone",
                schema: "catalog",
                table: "change_history",
                column: "user_phone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "change_history",
                schema: "catalog");
        }
    }
}