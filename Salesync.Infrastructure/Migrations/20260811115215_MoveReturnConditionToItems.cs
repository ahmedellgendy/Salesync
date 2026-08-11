using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salesync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveReturnConditionToItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Condition",
                table: "InvoiceReturns");

            migrationBuilder.AddColumn<int>(
                name: "Condition",
                table: "InvoiceReturnItems",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Condition",
                table: "InvoiceReturnItems");

            migrationBuilder.AddColumn<int>(
                name: "Condition",
                table: "InvoiceReturns",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
