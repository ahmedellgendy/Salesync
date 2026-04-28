using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salesync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCustomerAndProductClasses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasDebt",
                table: "Customers");

            migrationBuilder.RenameColumn(
                name: "RouteId",
                table: "Customers",
                newName: "TotalOrders");

            migrationBuilder.RenameColumn(
                name: "CreditBalance",
                table: "Customers",
                newName: "TotalPurchaseAmount");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercentage",
                table: "Products",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxStockLevel",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinStockLevel",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SKU",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "WarehouseId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Customers",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CreditLimit",
                table: "Customers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentBalance",
                table: "Customers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPurchaseDate",
                table: "Customers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Products_WarehouseId",
                table: "Products",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_BranchId",
                table: "Customers",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Branches_BranchId",
                table: "Customers",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Warehouses_WarehouseId",
                table: "Products",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Branches_BranchId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Warehouses_WarehouseId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_WarehouseId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Customers_BranchId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DiscountPercentage",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MaxStockLevel",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MinStockLevel",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SKU",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CreditLimit",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CurrentBalance",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "LastPurchaseDate",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Customers");

            migrationBuilder.RenameColumn(
                name: "TotalPurchaseAmount",
                table: "Customers",
                newName: "CreditBalance");

            migrationBuilder.RenameColumn(
                name: "TotalOrders",
                table: "Customers",
                newName: "RouteId");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "HasDebt",
                table: "Customers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
