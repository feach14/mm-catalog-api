using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Catalog.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPricePercentages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "price_percentages",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    value = table.Column<int>(type: "integer", nullable: false),
                    applies_to = table.Column<string>(type: "text", nullable: false),
                    price_type = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price_percentages", x => x.id);
                });

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "price_percentages",
                columns: new[] { "id", "applies_to", "description", "key", "name", "price_type", "value" },
                values: new object[,]
                {
                    { 1, "MATERIAL", "Увеличивает базовую стоимость листового материала для частного клиента на указанный процент.", "MATERIAL_CUSTOM", "Надбавка на материал для частного клиента", "CUSTOM", 0 },
                    { 2, "MATERIAL", "Увеличивает базовую стоимость листового материала для оптового клиента на указанный процент.", "MATERIAL_MEBEL", "Надбавка на материал для оптового клиента", "MEBEL", 0 },
                    { 3, "MATERIAL", "Увеличивает базовую стоимость листового материала для индивидуального клиента на указанный процент.", "MATERIAL_KEY", "Надбавка на материал для индивидуального клиента", "KEY", 0 },
                    { 7, "EDGE", "Увеличивает базовую стоимость кромки толщиной 0,4 мм для частного клиента на указанный процент.", "EDGE_04_CUSTOM", "Надбавка на кромку 0,4 мм для частного клиента", "CUSTOM", 0 },
                    { 8, "EDGE", "Увеличивает базовую стоимость кромки толщиной 0,4 мм для оптового клиента на указанный процент.", "EDGE_04_MEBEL", "Надбавка на кромку 0,4 мм для оптового клиента", "MEBEL", 0 },
                    { 9, "EDGE", "Увеличивает базовую стоимость кромки толщиной 0,4 мм для индивидуального клиента на указанный процент.", "EDGE_04_KEY", "Надбавка на кромку 0,4 мм для индивидуального клиента", "KEY", 0 },
                    { 10, "EDGE", "Увеличивает базовую стоимость кромки толщиной 1 мм для частного клиента на указанный процент.", "EDGE_1_CUSTOM", "Надбавка на кромку 1 мм для частного клиента", "CUSTOM", 0 },
                    { 11, "EDGE", "Увеличивает базовую стоимость кромки толщиной 1 мм для оптового клиента на указанный процент.", "EDGE_1_MEBEL", "Надбавка на кромку 1 мм для оптового клиента", "MEBEL", 0 },
                    { 12, "EDGE", "Увеличивает базовую стоимость кромки толщиной 1 мм для индивидуального клиента на указанный процент.", "EDGE_1_KEY", "Надбавка на кромку 1 мм для индивидуального клиента", "KEY", 0 },
                    { 13, "EDGE", "Увеличивает базовую стоимость кромки толщиной 2 мм для частного клиента на указанный процент.", "EDGE_2_CUSTOM", "Надбавка на кромку 2 мм для частного клиента", "CUSTOM", 0 },
                    { 14, "EDGE", "Увеличивает базовую стоимость кромки толщиной 2 мм для оптового клиента на указанный процент.", "EDGE_2_MEBEL", "Надбавка на кромку 2 мм для оптового клиента", "MEBEL", 0 },
                    { 15, "EDGE", "Увеличивает базовую стоимость кромки толщиной 2 мм для индивидуального клиента на указанный процент.", "EDGE_2_KEY", "Надбавка на кромку 2 мм для индивидуального клиента", "KEY", 0 }
                });

            migrationBuilder.CreateIndex(
                name: "ix_price_percentages_key",
                schema: "catalog",
                table: "price_percentages",
                column: "key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "price_percentages",
                schema: "catalog");
        }
    }
}