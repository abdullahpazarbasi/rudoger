using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rudoger.Modules.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InventoryCreationIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreationIdempotencyKey",
                schema: "inventory",
                table: "StockItems",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "OpeningQuantity",
                schema: "inventory",
                table: "StockItems",
                type: "decimal(19,6)",
                precision: 19,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_StockItems_CreationIdempotencyKey",
                schema: "inventory",
                table: "StockItems",
                column: "CreationIdempotencyKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockItems_CreationIdempotencyKey",
                schema: "inventory",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "CreationIdempotencyKey",
                schema: "inventory",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "OpeningQuantity",
                schema: "inventory",
                table: "StockItems");
        }
    }
}
