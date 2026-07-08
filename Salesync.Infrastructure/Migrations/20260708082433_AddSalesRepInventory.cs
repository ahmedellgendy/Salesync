using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salesync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesRepInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesRepInventories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesRepId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesRepInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesRepInventories_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesRepInventories_SalesReps_SalesRepId",
                        column: x => x.SalesRepId,
                        principalTable: "SalesReps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesRepInventoryMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesRepId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    MovementType = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<int>(type: "int", nullable: true),
                    SourceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MovementDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesRepInventoryMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesRepInventoryMovements_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesRepInventoryMovements_SalesReps_SalesRepId",
                        column: x => x.SalesRepId,
                        principalTable: "SalesReps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepInventories_ProductId",
                table: "SalesRepInventories",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepInventories_SalesRepId_ProductId",
                table: "SalesRepInventories",
                columns: new[] { "SalesRepId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepInventoryMovements_MovementDate",
                table: "SalesRepInventoryMovements",
                column: "MovementDate");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepInventoryMovements_ProductId",
                table: "SalesRepInventoryMovements",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepInventoryMovements_SalesRepId",
                table: "SalesRepInventoryMovements",
                column: "SalesRepId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepInventoryMovements_SourceId",
                table: "SalesRepInventoryMovements",
                column: "SourceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesRepInventories");

            migrationBuilder.DropTable(
                name: "SalesRepInventoryMovements");
        }
    }
}
