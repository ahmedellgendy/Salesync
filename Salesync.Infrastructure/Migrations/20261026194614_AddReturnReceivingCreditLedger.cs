using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salesync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnReceivingCreditLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CreditAppliedAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "InvoiceReturnReceipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreditNoteNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InvoiceReturnId = table.Column<int>(type: "int", nullable: false),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    SalesRepId = table.Column<int>(type: "int", nullable: true),
                    SalesRepSessionId = table.Column<int>(type: "int", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AppliedToInvoiceAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CustomerCreditAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CloseRemaining = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceReturnReceipts", x => x.Id);
                    table.CheckConstraint("CK_ReturnReceipt_Amounts", "[TotalAmount] >= 0 AND [AppliedToInvoiceAmount] >= 0 AND [CustomerCreditAmount] >= 0 AND [TotalAmount] = [AppliedToInvoiceAmount] + [CustomerCreditAmount]");
                    table.ForeignKey(
                        name: "FK_InvoiceReturnReceipts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceReturnReceipts_InvoiceReturns_InvoiceReturnId",
                        column: x => x.InvoiceReturnId,
                        principalTable: "InvoiceReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceReturnReceipts_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceReturnReceipts_SalesReps_SalesRepId",
                        column: x => x.SalesRepId,
                        principalTable: "SalesReps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceReturnReceiptItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceiptId = table.Column<int>(type: "int", nullable: false),
                    InvoiceReturnItemId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    BonusQuantity = table.Column<int>(type: "int", nullable: false),
                    Condition = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceReturnReceiptItems", x => x.Id);
                    table.CheckConstraint("CK_ReturnReceiptItem_Quantity", "[Quantity] >= 0 AND [BonusQuantity] >= 0 AND ([Quantity] > 0 OR [BonusQuantity] > 0) AND [TotalAmount] >= 0");
                    table.ForeignKey(
                        name: "FK_InvoiceReturnReceiptItems_InvoiceReturnItems_InvoiceReturnItemId",
                        column: x => x.InvoiceReturnItemId,
                        principalTable: "InvoiceReturnItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceReturnReceiptItems_InvoiceReturnReceipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "InvoiceReturnReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceReturnReceiptItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceReturnReceiptItems_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReturnReceiptItems_InvoiceReturnItemId",
                table: "InvoiceReturnReceiptItems",
                column: "InvoiceReturnItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReturnReceiptItems_ProductId",
                table: "InvoiceReturnReceiptItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReturnReceiptItems_ReceiptId_InvoiceReturnItemId",
                table: "InvoiceReturnReceiptItems",
                columns: new[] { "ReceiptId", "InvoiceReturnItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReturnReceiptItems_WarehouseId",
                table: "InvoiceReturnReceiptItems",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReturnReceipts_CreditNoteNumber",
                table: "InvoiceReturnReceipts",
                column: "CreditNoteNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReturnReceipts_CustomerId_CreatedAt",
                table: "InvoiceReturnReceipts",
                columns: new[] { "CustomerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReturnReceipts_InvoiceId",
                table: "InvoiceReturnReceipts",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReturnReceipts_InvoiceReturnId",
                table: "InvoiceReturnReceipts",
                column: "InvoiceReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReturnReceipts_OperationId",
                table: "InvoiceReturnReceipts",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReturnReceipts_ReceiptNumber",
                table: "InvoiceReturnReceipts",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceReturnReceipts_SalesRepId",
                table: "InvoiceReturnReceipts",
                column: "SalesRepId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceReturnReceiptItems");

            migrationBuilder.DropTable(
                name: "InvoiceReturnReceipts");

            migrationBuilder.DropColumn(
                name: "CreditAppliedAmount",
                table: "Invoices");
        }
    }
}
