using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DFTRK.Migrations
{
    /// <inheritdoc />
    public partial class FixWholesalerProductNavigationProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_WholesalerProducts_WholesalerProductId1",
                table: "CartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_WholesalerProducts_WholesalerProductId1",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_WholesalerProductId1",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_WholesalerProductId1",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "WholesalerProductId1",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "WholesalerProductId1",
                table: "CartItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WholesalerProductId1",
                table: "OrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WholesalerProductId1",
                table: "CartItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_WholesalerProductId1",
                table: "OrderItems",
                column: "WholesalerProductId1");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_WholesalerProductId1",
                table: "CartItems",
                column: "WholesalerProductId1");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_WholesalerProducts_WholesalerProductId1",
                table: "CartItems",
                column: "WholesalerProductId1",
                principalTable: "WholesalerProducts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_WholesalerProducts_WholesalerProductId1",
                table: "OrderItems",
                column: "WholesalerProductId1",
                principalTable: "WholesalerProducts",
                principalColumn: "Id");
        }
    }
}
