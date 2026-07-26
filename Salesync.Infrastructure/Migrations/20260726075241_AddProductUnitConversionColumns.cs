using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salesync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductUnitConversionColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LargeUnit",
                table: "Products",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "كرتونة");

            migrationBuilder.AddColumn<string>(
                name: "SmallUnit",
                table: "Products",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "قطعة");

            migrationBuilder.AddColumn<int>(
                name: "UnitsPerLargeUnit",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LargeUnit",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SmallUnit",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UnitsPerLargeUnit",
                table: "Products");
        }
    }
}
