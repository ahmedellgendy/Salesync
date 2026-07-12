using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salesync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesRepDayClosing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesRepDayClosings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClosingNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SalesRepId = table.Column<int>(type: "int", nullable: false),
                    SalesRepSessionId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    ClosingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalSalesAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCollectionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalReturnAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpectedCashAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualCashAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CashVariance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpectedTotalRemainingQuantity = table.Column<int>(type: "int", nullable: false),
                    ActualTotalReturnedQuantity = table.Column<int>(type: "int", nullable: false),
                    TotalVarianceQuantity = table.Column<int>(type: "int", nullable: false),
                    SubmittedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CancelledByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesRepDayClosings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesRepDayClosings_SalesRepSessions_SalesRepSessionId",
                        column: x => x.SalesRepSessionId,
                        principalTable: "SalesRepSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesRepDayClosings_SalesReps_SalesRepId",
                        column: x => x.SalesRepId,
                        principalTable: "SalesReps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesRepDayClosings_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesRepDayClosingItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesRepDayClosingId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ExpectedRemainingQuantity = table.Column<int>(type: "int", nullable: false),
                    ActualReturnedQuantity = table.Column<int>(type: "int", nullable: false),
                    VarianceQuantity = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesRepDayClosingItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesRepDayClosingItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesRepDayClosingItems_SalesRepDayClosings_SalesRepDayClosingId",
                        column: x => x.SalesRepDayClosingId,
                        principalTable: "SalesRepDayClosings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepDayClosingItems_ProductId",
                table: "SalesRepDayClosingItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepDayClosingItems_SalesRepDayClosingId",
                table: "SalesRepDayClosingItems",
                column: "SalesRepDayClosingId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepDayClosingItems_SalesRepDayClosingId_ProductId",
                table: "SalesRepDayClosingItems",
                columns: new[] { "SalesRepDayClosingId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepDayClosings_ClosingDate",
                table: "SalesRepDayClosings",
                column: "ClosingDate");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepDayClosings_ClosingNumber",
                table: "SalesRepDayClosings",
                column: "ClosingNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepDayClosings_SalesRepId",
                table: "SalesRepDayClosings",
                column: "SalesRepId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepDayClosings_SalesRepSessionId",
                table: "SalesRepDayClosings",
                column: "SalesRepSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepDayClosings_Status",
                table: "SalesRepDayClosings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepDayClosings_WarehouseId",
                table: "SalesRepDayClosings",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesRepDayClosingItems");

            migrationBuilder.DropTable(
                name: "SalesRepDayClosings");
        }
    }
}
