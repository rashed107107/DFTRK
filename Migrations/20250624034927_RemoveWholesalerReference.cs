using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DFTRK.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWholesalerReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RetailerPartnerships_AspNetUsers_WholesalerId",
                table: "RetailerPartnerships");

            migrationBuilder.DropIndex(
                name: "IX_RetailerPartnerships_WholesalerId",
                table: "RetailerPartnerships");

            migrationBuilder.DropColumn(
                name: "WholesalerId",
                table: "RetailerPartnerships");

            migrationBuilder.AddColumn<string>(
                name: "WholesalerName",
                table: "RetailerPartnerships",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WholesalerName",
                table: "RetailerPartnerships");

            migrationBuilder.AddColumn<string>(
                name: "WholesalerId",
                table: "RetailerPartnerships",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_RetailerPartnerships_WholesalerId",
                table: "RetailerPartnerships",
                column: "WholesalerId");

            migrationBuilder.AddForeignKey(
                name: "FK_RetailerPartnerships_AspNetUsers_WholesalerId",
                table: "RetailerPartnerships",
                column: "WholesalerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
