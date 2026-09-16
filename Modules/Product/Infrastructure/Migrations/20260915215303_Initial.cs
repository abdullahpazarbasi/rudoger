using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rudoger.Modules.Product.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "product");

            migrationBuilder.CreateTable(
                name: "Events",
                schema: "product",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StreamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SchemaVersion = table.Column<int>(type: "int", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                schema: "product",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BaseUomCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    BasePriceAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    BasePriceCurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductUsageClaims",
                schema: "product",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsageType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductUsageClaims", x => new { x.ProductId, x.OperationId });
                });

            migrationBuilder.CreateTable(
                name: "ProductPackagings",
                schema: "product",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    UomCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    WeightInKg = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    LengthInMm = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    WidthInMm = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    HeightInMm = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPackagings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductPackagings_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "product",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_AggregateType_OccurredAtUtc",
                schema: "product",
                table: "Events",
                columns: new[] { "AggregateType", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_StreamId_Version",
                schema: "product",
                table: "Events",
                columns: new[] { "StreamId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackagings_Barcode",
                schema: "product",
                table: "ProductPackagings",
                column: "Barcode",
                unique: true,
                filter: "[Barcode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackagings_ProductId_Level",
                schema: "product",
                table: "ProductPackagings",
                columns: new[] { "ProductId", "Level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackagings_ProductId_UomCode",
                schema: "product",
                table: "ProductPackagings",
                columns: new[] { "ProductId", "UomCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Sku",
                schema: "product",
                table: "Products",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductUsageClaims_OperationId",
                schema: "product",
                table: "ProductUsageClaims",
                column: "OperationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Events",
                schema: "product");

            migrationBuilder.DropTable(
                name: "ProductPackagings",
                schema: "product");

            migrationBuilder.DropTable(
                name: "ProductUsageClaims",
                schema: "product");

            migrationBuilder.DropTable(
                name: "Products",
                schema: "product");
        }
    }
}
