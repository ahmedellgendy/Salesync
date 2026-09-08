using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salesync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDayClosingFinancialBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CashCollectionAmount",
                table: "SalesRepDayClosings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NonCashCollectionAmount",
                table: "SalesRepDayClosings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OutstandingAmount",
                table: "SalesRepDayClosings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CashCollectionAmount",
                table: "SalesRepDayClosings");

            migrationBuilder.DropColumn(
                name: "NonCashCollectionAmount",
                table: "SalesRepDayClosings");

            migrationBuilder.DropColumn(
                name: "OutstandingAmount",
                table: "SalesRepDayClosings");
        }
    }
}
