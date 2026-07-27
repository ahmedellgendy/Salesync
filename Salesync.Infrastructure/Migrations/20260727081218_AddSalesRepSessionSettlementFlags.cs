using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salesync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesRepSessionSettlementFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsStockSettled",
                table: "SalesRepSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTreasurySettled",
                table: "SalesRepSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "StockSettledAt",
                table: "SalesRepSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TreasurySettledAt",
                table: "SalesRepSessions",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsStockSettled",
                table: "SalesRepSessions");

            migrationBuilder.DropColumn(
                name: "IsTreasurySettled",
                table: "SalesRepSessions");

            migrationBuilder.DropColumn(
                name: "StockSettledAt",
                table: "SalesRepSessions");

            migrationBuilder.DropColumn(
                name: "TreasurySettledAt",
                table: "SalesRepSessions");
        }
    }
}
