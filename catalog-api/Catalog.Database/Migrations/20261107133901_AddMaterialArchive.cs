using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_material_edge_link_markings_material_edge_links_material_id",
                schema: "catalog",
                table: "material_edge_link_markings");

            migrationBuilder.DropPrimaryKey(
                name: "pk_material_edge_links",
                schema: "catalog",
                table: "material_edge_links");

            migrationBuilder.DropPrimaryKey(
                name: "pk_material_edge_link_markings",
                schema: "catalog",
                table: "material_edge_link_markings");

            migrationBuilder.AlterColumn<int>(
                name: "material_id",
                schema: "catalog",
                table: "material_images",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "archived_material_id",
                schema: "catalog",
                table: "material_images",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "material_id",
                schema: "catalog",
                table: "material_edge_links",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "id",
                schema: "catalog",
                table: "material_edge_links",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "archived_material_id",
                schema: "catalog",
                table: "material_edge_links",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id",
                schema: "catalog",
                table: "material_edge_link_markings",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "owner_material_id",
                schema: "catalog",
                table: "material_edge_links",
                type: "integer",
                nullable: false,
                computedColumnSql: "COALESCE(material_id, archived_material_id)",
                stored: true);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_material_edge_links_owner_material_id_edge_id",
                schema: "catalog",
                table: "material_edge_links",
                columns: new[] { "owner_material_id", "edge_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_material_edge_links",
                schema: "catalog",
                table: "material_edge_links",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_material_edge_link_markings",
                schema: "catalog",
                table: "material_edge_link_markings",
                column: "id");

            migrationBuilder.CreateTable(
                name: "archive_materials",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    original_material_id = table.Column<int>(type: "integer", nullable: false),
                    category_id = table.Column<int>(type: "integer", nullable: false),
                    material_sheet_size_id = table.Column<int>(type: "integer", nullable: false),
                    material_type_id = table.Column<int>(type: "integer", nullable: false),
                    material_manufacturer_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    article = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    material_thickness_id = table.Column<int>(type: "integer", nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false),
                    external_link = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    comment_on_material_is_required = table.Column<bool>(type: "boolean", nullable: false),
                    allow_second_item_in_order = table.Column<bool>(type: "boolean", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    stroydvor_product_id = table.Column<int>(type: "integer", nullable: true),
                    stroydvor_article = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    stroydvor_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    stroydvor_last_sync_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    stroydvor_in_stock = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    hide_on_site = table.Column<bool>(type: "boolean", nullable: false),
                    hide_in_lk = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    hide_price_on_site = table.Column<bool>(type: "boolean", nullable: false),
                    count_type_enum = table.Column<int>(type: "integer", nullable: false),
                    order_by_col = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_archive_materials", x => x.id);
                    table.ForeignKey(
                        name: "fk_archive_materials_material_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "catalog",
                        principalTable: "material_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_archive_materials_material_manufacturers_material_manufactu",
                        column: x => x.material_manufacturer_id,
                        principalSchema: "catalog",
                        principalTable: "material_manufacturers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_archive_materials_material_sheet_sizes_material_sheet_size_",
                        column: x => x.material_sheet_size_id,
                        principalSchema: "catalog",
                        principalTable: "material_sheet_sizes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_archive_materials_material_thicknesses_material_thickness_id",
                        column: x => x.material_thickness_id,
                        principalSchema: "catalog",
                        principalTable: "material_thicknesses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_archive_materials_material_types_material_type_id",
                        column: x => x.material_type_id,
                        principalSchema: "catalog",
                        principalTable: "material_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_material_images_archived_material_id_image_type",
                schema: "catalog",
                table: "material_images",
                columns: new[] { "archived_material_id", "image_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_material_edge_links_archived_material_id_edge_id",
                schema: "catalog",
                table: "material_edge_links",
                columns: new[] { "archived_material_id", "edge_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_material_edge_links_archived_material_id_priority",
                schema: "catalog",
                table: "material_edge_links",
                columns: new[] { "archived_material_id", "priority" });

            migrationBuilder.CreateIndex(
                name: "ix_material_edge_links_material_id_edge_id",
                schema: "catalog",
                table: "material_edge_links",
                columns: new[] { "material_id", "edge_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_material_edge_link_markings_material_id_edge_id_marking_id",
                schema: "catalog",
                table: "material_edge_link_markings",
                columns: new[] { "material_id", "edge_id", "marking_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_archive_materials_category_id",
                schema: "catalog",
                table: "archive_materials",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_archive_materials_material_manufacturer_id",
                schema: "catalog",
                table: "archive_materials",
                column: "material_manufacturer_id");

            migrationBuilder.CreateIndex(
                name: "ix_archive_materials_material_sheet_size_id",
                schema: "catalog",
                table: "archive_materials",
                column: "material_sheet_size_id");

            migrationBuilder.CreateIndex(
                name: "ix_archive_materials_material_thickness_id",
                schema: "catalog",
                table: "archive_materials",
                column: "material_thickness_id");

            migrationBuilder.CreateIndex(
                name: "ix_archive_materials_material_type_id",
                schema: "catalog",
                table: "archive_materials",
                column: "material_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_archive_materials_order_by_col",
                schema: "catalog",
                table: "archive_materials",
                column: "order_by_col",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_archive_materials_stroydvor_product_id",
                schema: "catalog",
                table: "archive_materials",
                column: "stroydvor_product_id");

            migrationBuilder.AddForeignKey(
                name: "fk_material_edge_link_markings_material_edge_links_material_id",
                schema: "catalog",
                table: "material_edge_link_markings",
                columns: new[] { "material_id", "edge_id" },
                principalSchema: "catalog",
                principalTable: "material_edge_links",
                principalColumns: new[] { "owner_material_id", "edge_id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_material_edge_links_archived_materials_archived_material_id",
                schema: "catalog",
                table: "material_edge_links",
                column: "archived_material_id",
                principalSchema: "catalog",
                principalTable: "archive_materials",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_material_images_archived_materials_archived_material_id",
                schema: "catalog",
                table: "material_images",
                column: "archived_material_id",
                principalSchema: "catalog",
                principalTable: "archive_materials",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_material_edge_link_markings_material_edge_links_material_id",
                schema: "catalog",
                table: "material_edge_link_markings");

            migrationBuilder.DropForeignKey(
                name: "fk_material_edge_links_archived_materials_archived_material_id",
                schema: "catalog",
                table: "material_edge_links");

            migrationBuilder.DropForeignKey(
                name: "fk_material_images_archived_materials_archived_material_id",
                schema: "catalog",
                table: "material_images");

            migrationBuilder.DropTable(
                name: "archive_materials",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "ix_material_images_archived_material_id_image_type",
                schema: "catalog",
                table: "material_images");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_material_edge_links_owner_material_id_edge_id",
                schema: "catalog",
                table: "material_edge_links");

            migrationBuilder.DropPrimaryKey(
                name: "pk_material_edge_links",
                schema: "catalog",
                table: "material_edge_links");

            migrationBuilder.DropIndex(
                name: "ix_material_edge_links_archived_material_id_edge_id",
                schema: "catalog",
                table: "material_edge_links");

            migrationBuilder.DropIndex(
                name: "ix_material_edge_links_archived_material_id_priority",
                schema: "catalog",
                table: "material_edge_links");

            migrationBuilder.DropIndex(
                name: "ix_material_edge_links_material_id_edge_id",
                schema: "catalog",
                table: "material_edge_links");

            migrationBuilder.DropPrimaryKey(
                name: "pk_material_edge_link_markings",
                schema: "catalog",
                table: "material_edge_link_markings");

            migrationBuilder.DropIndex(
                name: "ix_material_edge_link_markings_material_id_edge_id_marking_id",
                schema: "catalog",
                table: "material_edge_link_markings");

            migrationBuilder.DropColumn(
                name: "owner_material_id",
                schema: "catalog",
                table: "material_edge_links");

            migrationBuilder.DropColumn(
                name: "archived_material_id",
                schema: "catalog",
                table: "material_images");

            migrationBuilder.DropColumn(
                name: "id",
                schema: "catalog",
                table: "material_edge_links");

            migrationBuilder.DropColumn(
                name: "archived_material_id",
                schema: "catalog",
                table: "material_edge_links");

            migrationBuilder.DropColumn(
                name: "id",
                schema: "catalog",
                table: "material_edge_link_markings");

            migrationBuilder.AlterColumn<int>(
                name: "material_id",
                schema: "catalog",
                table: "material_images",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "material_id",
                schema: "catalog",
                table: "material_edge_links",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_material_edge_links",
                schema: "catalog",
                table: "material_edge_links",
                columns: new[] { "material_id", "edge_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_material_edge_link_markings",
                schema: "catalog",
                table: "material_edge_link_markings",
                columns: new[] { "material_id", "edge_id", "marking_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_material_edge_link_markings_material_edge_links_material_id",
                schema: "catalog",
                table: "material_edge_link_markings",
                columns: new[] { "material_id", "edge_id" },
                principalSchema: "catalog",
                principalTable: "material_edge_links",
                principalColumns: new[] { "material_id", "edge_id" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}