using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salesync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceItemRelationToInvoiceReturnItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InvoiceItemId",
                table: "InvoiceReturnItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReturnItems_InvoiceItemId",
                table: "InvoiceReturnItems",
                column: "InvoiceItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceReturnItems_InvoiceItems_InvoiceItemId",
                table: "InvoiceReturnItems",
                column: "InvoiceItemId",
                principalTable: "InvoiceItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceReturnItems_InvoiceItems_InvoiceItemId",
                table: "InvoiceReturnItems");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceReturnItems_InvoiceItemId",
                table: "InvoiceReturnItems");

            migrationBuilder.DropColumn(
                name: "InvoiceItemId",
                table: "InvoiceReturnItems");
        }
    }
}
