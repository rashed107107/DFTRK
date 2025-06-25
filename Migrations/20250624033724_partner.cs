using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DFTRK.Migrations
{
    /// <inheritdoc />
    public partial class partner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RetailerPartnerships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RetailerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WholesalerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PartnershipName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetailerPartnerships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetailerPartnerships_AspNetUsers_RetailerId",
                        column: x => x.RetailerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetailerPartnerships_AspNetUsers_WholesalerId",
                        column: x => x.WholesalerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RetailerPartnerCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartnershipId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetailerPartnerCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetailerPartnerCategories_RetailerPartnerships_PartnershipId",
                        column: x => x.PartnershipId,
                        principalTable: "RetailerPartnerships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RetailerPartnerProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartnershipId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SKU = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CostPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SellingPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    MinimumStock = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetailerPartnerProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetailerPartnerProducts_RetailerPartnerCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "RetailerPartnerCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetailerPartnerProducts_RetailerPartnerships_PartnershipId",
                        column: x => x.PartnershipId,
                        principalTable: "RetailerPartnerships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RetailerPartnerCategories_PartnershipId",
                table: "RetailerPartnerCategories",
                column: "PartnershipId");

            migrationBuilder.CreateIndex(
                name: "IX_RetailerPartnerProducts_CategoryId",
                table: "RetailerPartnerProducts",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RetailerPartnerProducts_PartnershipId",
                table: "RetailerPartnerProducts",
                column: "PartnershipId");

            migrationBuilder.CreateIndex(
                name: "IX_RetailerPartnerships_RetailerId",
                table: "RetailerPartnerships",
                column: "RetailerId");

            migrationBuilder.CreateIndex(
                name: "IX_RetailerPartnerships_WholesalerId",
                table: "RetailerPartnerships",
                column: "WholesalerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RetailerPartnerProducts");

            migrationBuilder.DropTable(
                name: "RetailerPartnerCategories");

            migrationBuilder.DropTable(
                name: "RetailerPartnerships");
        }
    }
}
