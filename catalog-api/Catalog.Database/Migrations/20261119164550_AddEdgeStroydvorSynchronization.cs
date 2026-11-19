using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddEdgeStroydvorSynchronization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "stroydvor_article",
                schema: "catalog",
                table: "edges",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "stroydvor_in_stock",
                schema: "catalog",
                table: "edges",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "stroydvor_last_sync_at",
                schema: "catalog",
                table: "edges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "stroydvor_price",
                schema: "catalog",
                table: "edges",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "stroydvor_product_id",
                schema: "catalog",
                table: "edges",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_edges_stroydvor_product_id",
                schema: "catalog",
                table: "edges",
                column: "stroydvor_product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_edges_stroydvor_product_id",
                schema: "catalog",
                table: "edges");

            migrationBuilder.DropColumn(
                name: "stroydvor_article",
                schema: "catalog",
                table: "edges");

            migrationBuilder.DropColumn(
                name: "stroydvor_in_stock",
                schema: "catalog",
                table: "edges");

            migrationBuilder.DropColumn(
                name: "stroydvor_last_sync_at",
                schema: "catalog",
                table: "edges");

            migrationBuilder.DropColumn(
                name: "stroydvor_price",
                schema: "catalog",
                table: "edges");

            migrationBuilder.DropColumn(
                name: "stroydvor_product_id",
                schema: "catalog",
                table: "edges");
        }
    }
}