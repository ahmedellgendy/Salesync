using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salesync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesRepModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesReps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesRepCode = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    Mobile = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CategoryCode = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    SalesRepType = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    OutOfRouteLimit = table.Column<int>(type: "int", nullable: true),
                    OutOfOrderLimit = table.Column<int>(type: "int", nullable: true),
                    AllowCreditOverride = table.Column<bool>(type: "bit", nullable: false),
                    ProofOfVisit = table.Column<bool>(type: "bit", nullable: false),
                    MaxVisitsWithoutProof = table.Column<int>(type: "int", nullable: true),
                    SupervisorId = table.Column<int>(type: "int", nullable: true),
                    BusinessUnitId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesReps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesReps_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReps_SalesReps_SupervisorId",
                        column: x => x.SupervisorId,
                        principalTable: "SalesReps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RouteCode = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegionCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DistrictCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CityCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AreaCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RouteChannel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RouteGTM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RouteCategory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BusinessUnitId = table.Column<int>(type: "int", nullable: true),
                    AssignedSalesRepId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Routes_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Routes_SalesReps_AssignedSalesRepId",
                        column: x => x.AssignedSalesRepId,
                        principalTable: "SalesReps",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RouteCustomers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RouteId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    VisitSequence = table.Column<int>(type: "int", nullable: false),
                    VisitDays = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteCustomers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteCustomers_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RouteCustomers_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesRepRoutes",
                columns: table => new
                {
                    SalesRepId = table.Column<int>(type: "int", nullable: false),
                    RouteId = table.Column<int>(type: "int", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UnassignedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Id = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesRepRoutes", x => new { x.SalesRepId, x.RouteId });
                    table.ForeignKey(
                        name: "FK_SalesRepRoutes_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesRepRoutes_SalesReps_SalesRepId",
                        column: x => x.SalesRepId,
                        principalTable: "SalesReps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RouteCustomers_CustomerId",
                table: "RouteCustomers",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteCustomers_RouteId_CustomerId",
                table: "RouteCustomers",
                columns: new[] { "RouteId", "CustomerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_AssignedSalesRepId",
                table: "Routes",
                column: "AssignedSalesRepId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_BranchId",
                table: "Routes",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_RouteCode",
                table: "Routes",
                column: "RouteCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesRepRoutes_RouteId",
                table: "SalesRepRoutes",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReps_BranchId",
                table: "SalesReps",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReps_SalesRepCode",
                table: "SalesReps",
                column: "SalesRepCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesReps_SupervisorId",
                table: "SalesReps",
                column: "SupervisorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RouteCustomers");

            migrationBuilder.DropTable(
                name: "SalesRepRoutes");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropTable(
                name: "SalesReps");
        }
    }
}
