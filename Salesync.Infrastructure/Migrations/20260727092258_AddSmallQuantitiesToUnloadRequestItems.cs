using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salesync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSmallQuantitiesToUnloadRequestItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConfirmedSmallQuantity",
                table: "SalesRepUnloadRequestItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RequestedSmallQuantity",
                table: "SalesRepUnloadRequestItems",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmedSmallQuantity",
                table: "SalesRepUnloadRequestItems");

            migrationBuilder.DropColumn(
                name: "RequestedSmallQuantity",
                table: "SalesRepUnloadRequestItems");
        }
    }
}
