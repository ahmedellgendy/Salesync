using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salesync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseCategoryToTreasuryTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExpenseCategoryId",
                table: "TreasuryTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TreasuryTransactions_ExpenseCategoryId",
                table: "TreasuryTransactions",
                column: "ExpenseCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_TreasuryTransactions_ExpenseCategories_ExpenseCategoryId",
                table: "TreasuryTransactions",
                column: "ExpenseCategoryId",
                principalTable: "ExpenseCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TreasuryTransactions_ExpenseCategories_ExpenseCategoryId",
                table: "TreasuryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_TreasuryTransactions_ExpenseCategoryId",
                table: "TreasuryTransactions");

            migrationBuilder.DropColumn(
                name: "ExpenseCategoryId",
                table: "TreasuryTransactions");
        }
    }
}
