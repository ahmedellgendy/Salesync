using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salesync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesRepProfileImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileImageUrl",
                table: "SalesReps",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileImageUrl",
                table: "SalesReps");
        }
    }
}
