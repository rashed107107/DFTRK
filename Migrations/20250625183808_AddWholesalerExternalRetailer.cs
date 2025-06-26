using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DFTRK.Migrations
{
    /// <inheritdoc />
    public partial class AddWholesalerExternalRetailer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WholesalerExternalRetailers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WholesalerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RetailerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContactInfo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WholesalerExternalRetailers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WholesalerExternalRetailers_AspNetUsers_WholesalerId",
                        column: x => x.WholesalerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WholesalerExternalRetailers_WholesalerId",
                table: "WholesalerExternalRetailers",
                column: "WholesalerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WholesalerExternalRetailers");
        }
    }
}
