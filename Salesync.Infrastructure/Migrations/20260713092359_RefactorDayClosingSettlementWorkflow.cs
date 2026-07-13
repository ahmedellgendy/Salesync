using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salesync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorDayClosingSettlementWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsStockReceived",
                table: "SalesRepDayClosings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StockNotes",
                table: "SalesRepDayClosings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StockReceivedAt",
                table: "SalesRepDayClosings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StockReceivedByUserId",
                table: "SalesRepDayClosings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsStockReceived",
                table: "SalesRepDayClosings");

            migrationBuilder.DropColumn(
                name: "StockNotes",
                table: "SalesRepDayClosings");

            migrationBuilder.DropColumn(
                name: "StockReceivedAt",
                table: "SalesRepDayClosings");

            migrationBuilder.DropColumn(
                name: "StockReceivedByUserId",
                table: "SalesRepDayClosings");
        }
    }
}
