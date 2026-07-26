using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salesync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLoadRequestItemUnitQuantities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovedLargeQuantity",
                table: "LoadRequestItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ConfirmedLargeQuantity",
                table: "LoadRequestItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LargeUnit",
                table: "LoadRequestItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RequestedLargeQuantity",
                table: "LoadRequestItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SmallUnit",
                table: "LoadRequestItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "UnitsPerLargeUnit",
                table: "LoadRequestItems",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedLargeQuantity",
                table: "LoadRequestItems");

            migrationBuilder.DropColumn(
                name: "ConfirmedLargeQuantity",
                table: "LoadRequestItems");

            migrationBuilder.DropColumn(
                name: "LargeUnit",
                table: "LoadRequestItems");

            migrationBuilder.DropColumn(
                name: "RequestedLargeQuantity",
                table: "LoadRequestItems");

            migrationBuilder.DropColumn(
                name: "SmallUnit",
                table: "LoadRequestItems");

            migrationBuilder.DropColumn(
                name: "UnitsPerLargeUnit",
                table: "LoadRequestItems");
        }
    }
}
