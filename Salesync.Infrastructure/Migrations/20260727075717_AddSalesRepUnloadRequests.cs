using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salesync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesRepUnloadRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesRepUnloadRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SalesRepId = table.Column<int>(type: "int", nullable: false),
                    SalesRepSessionId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ConfirmedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CancelledByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TotalRequestedQuantity = table.Column<int>(type: "int", nullable: false),
                    TotalConfirmedQuantity = table.Column<int>(type: "int", nullable: false),
                    TotalVarianceQuantity = table.Column<int>(type: "int", nullable: false),
                    TotalItems = table.Column<int>(type: "int", nullable: false),
                    SalesRepNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WarehouseNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesRepUnloadRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesRepUnloadRequestItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesRepUnloadRequestId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SmallUnit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LargeUnit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UnitsPerLargeUnit = table.Column<int>(type: "int", nullable: false),
                    RequestedLargeQuantity = table.Column<int>(type: "int", nullable: false),
                    RequestedQuantity = table.Column<int>(type: "int", nullable: false),
                    ConfirmedLargeQuantity = table.Column<int>(type: "int", nullable: false),
                    ConfirmedQuantity = table.Column<int>(type: "int", nullable: false),
                    VarianceQuantity = table.Column<int>(type: "int", nullable: false),
                    SalesRepInventoryBeforeUnload = table.Column<int>(type: "int", nullable: false),
                    SalesRepInventoryAfterUnload = table.Column<int>(type: "int", nullable: false),
                    SalesRepNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WarehouseNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesRepUnloadRequestItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesRepUnloadRequestItems_SalesRepUnloadRequests_SalesRepUnloadRequestId",
                        column: x => x.SalesRepUnloadRequestId,
                        principalTable: "SalesRepUnloadRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepUnloadRequestItems_ProductId",
                table: "SalesRepUnloadRequestItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepUnloadRequestItems_SalesRepUnloadRequestId",
                table: "SalesRepUnloadRequestItems",
                column: "SalesRepUnloadRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepUnloadRequests_BranchId",
                table: "SalesRepUnloadRequests",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepUnloadRequests_RequestedAt",
                table: "SalesRepUnloadRequests",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepUnloadRequests_RequestNumber",
                table: "SalesRepUnloadRequests",
                column: "RequestNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepUnloadRequests_SalesRepId",
                table: "SalesRepUnloadRequests",
                column: "SalesRepId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepUnloadRequests_SalesRepSessionId",
                table: "SalesRepUnloadRequests",
                column: "SalesRepSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepUnloadRequests_Status",
                table: "SalesRepUnloadRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepUnloadRequests_WarehouseId",
                table: "SalesRepUnloadRequests",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesRepUnloadRequestItems");

            migrationBuilder.DropTable(
                name: "SalesRepUnloadRequests");
        }
    }
}
