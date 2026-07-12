using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salesync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDayClosingUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesRepDayClosings_SalesRepSessionId",
                table: "SalesRepDayClosings");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepDayClosings_SalesRepSessionId",
                table: "SalesRepDayClosings",
                column: "SalesRepSessionId",
                unique: true,
                filter: "[IsActive] = 1 AND [Status] IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesRepDayClosings_SalesRepSessionId",
                table: "SalesRepDayClosings");

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepDayClosings_SalesRepSessionId",
                table: "SalesRepDayClosings",
                column: "SalesRepSessionId",
                unique: true);
        }
    }
}
