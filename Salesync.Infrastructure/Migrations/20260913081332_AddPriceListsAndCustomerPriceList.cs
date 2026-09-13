using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salesync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceListsAndCustomerPriceList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceId",
                table: "Customers");

            migrationBuilder.AddColumn<int>(
                name: "PriceListId",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_PriceListId",
                table: "Customers",
                column: "PriceListId");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_PriceLists_PriceListId",
                table: "Customers",
                column: "PriceListId",
                principalTable: "PriceLists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_PriceLists_PriceListId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_PriceListId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PriceListId",
                table: "Customers");

            migrationBuilder.AddColumn<string>(
                name: "PriceId",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
