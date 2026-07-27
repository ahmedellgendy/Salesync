using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salesync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceItemUnitQuantities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BonusLargeQuantity",
                table: "InvoiceItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LargeUnit",
                table: "InvoiceItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SaleLargeQuantity",
                table: "InvoiceItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SmallUnit",
                table: "InvoiceItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "UnitsPerLargeUnit",
                table: "InvoiceItems",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BonusLargeQuantity",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "LargeUnit",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "SaleLargeQuantity",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "SmallUnit",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "UnitsPerLargeUnit",
                table: "InvoiceItems");
        }
    }
}
