using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salesync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTreasuryModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CashNotes",
                table: "SalesRepDayClosings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CashReceivedAt",
                table: "SalesRepDayClosings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CashReceivedByUserId",
                table: "SalesRepDayClosings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCashReceived",
                table: "SalesRepDayClosings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CashBoxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashBoxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashBoxes_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CashReceipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CashBoxId = table.Column<int>(type: "int", nullable: false),
                    SalesRepId = table.Column<int>(type: "int", nullable: false),
                    SalesRepSessionId = table.Column<int>(type: "int", nullable: false),
                    SalesRepDayClosingId = table.Column<int>(type: "int", nullable: false),
                    ExpectedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReceivedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VarianceAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReceivedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashReceipts_CashBoxes_CashBoxId",
                        column: x => x.CashBoxId,
                        principalTable: "CashBoxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashReceipts_SalesRepDayClosings_SalesRepDayClosingId",
                        column: x => x.SalesRepDayClosingId,
                        principalTable: "SalesRepDayClosings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashReceipts_SalesRepSessions_SalesRepSessionId",
                        column: x => x.SalesRepSessionId,
                        principalTable: "SalesRepSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashReceipts_SalesReps_SalesRepId",
                        column: x => x.SalesRepId,
                        principalTable: "SalesReps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesRepCashLedgers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesRepId = table.Column<int>(type: "int", nullable: false),
                    EntryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    CashReceiptId = table.Column<int>(type: "int", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesRepCashLedgers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesRepCashLedgers_CashReceipts_CashReceiptId",
                        column: x => x.CashReceiptId,
                        principalTable: "CashReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesRepCashLedgers_SalesReps_SalesRepId",
                        column: x => x.SalesRepId,
                        principalTable: "SalesReps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashBoxes_BranchId",
                table: "CashBoxes",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_CashBoxes_Code",
                table: "CashBoxes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashReceipts_CashBoxId",
                table: "CashReceipts",
                column: "CashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_CashReceipts_ReceiptNumber",
                table: "CashReceipts",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashReceipts_SalesRepDayClosingId",
                table: "CashReceipts",
                column: "SalesRepDayClosingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashReceipts_SalesRepId",
                table: "CashReceipts",
                column: "SalesRepId");

            migrationBuilder.CreateIndex(
                name: "IX_CashReceipts_SalesRepSessionId",
                table: "CashReceipts",
                column: "SalesRepSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepCashLedgers_CashReceiptId",
                table: "SalesRepCashLedgers",
                column: "CashReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepCashLedgers_SalesRepId",
                table: "SalesRepCashLedgers",
                column: "SalesRepId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesRepCashLedgers");

            migrationBuilder.DropTable(
                name: "CashReceipts");

            migrationBuilder.DropTable(
                name: "CashBoxes");

            migrationBuilder.DropColumn(
                name: "CashNotes",
                table: "SalesRepDayClosings");

            migrationBuilder.DropColumn(
                name: "CashReceivedAt",
                table: "SalesRepDayClosings");

            migrationBuilder.DropColumn(
                name: "CashReceivedByUserId",
                table: "SalesRepDayClosings");

            migrationBuilder.DropColumn(
                name: "IsCashReceived",
                table: "SalesRepDayClosings");
        }
    }
}
