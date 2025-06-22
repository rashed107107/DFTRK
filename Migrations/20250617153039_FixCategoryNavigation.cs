using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DFTRK.Migrations
{
    /// <inheritdoc />
    public partial class FixCategoryNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_WholesalerProducts_WholesalerProductId",
                table: "CartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Carts_AspNetUsers_RetailerId",
                table: "Carts");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_WholesalerProducts_WholesalerProductId",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_RetailerProducts_WholesalerProducts_WholesalerProductId",
                table: "RetailerProducts");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_AspNetUsers_RetailerId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_AspNetUsers_WholesalerId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Orders_OrderId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_WholesalerProducts_Products_ProductId",
                table: "WholesalerProducts");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_OrderId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_RetailerId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_WholesalerId",
                table: "Transactions");

            migrationBuilder.AlterColumn<string>(
                name: "WholesalerId",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "RetailerId",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<int>(
                name: "OrderId1",
                table: "Transactions",
                type: "int",
                nullable: true);

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
                name: "IX_Transactions_OrderId",
                table: "Transactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_OrderId1",
                table: "Transactions",
                column: "OrderId1",
                unique: true,
                filter: "[OrderId1] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_WholesalerProductId1",
                table: "OrderItems",
                column: "WholesalerProductId1");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_WholesalerProductId1",
                table: "CartItems",
                column: "WholesalerProductId1");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_WholesalerProducts_WholesalerProductId",
                table: "CartItems",
                column: "WholesalerProductId",
                principalTable: "WholesalerProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_WholesalerProducts_WholesalerProductId1",
                table: "CartItems",
                column: "WholesalerProductId1",
                principalTable: "WholesalerProducts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Carts_AspNetUsers_RetailerId",
                table: "Carts",
                column: "RetailerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_WholesalerProducts_WholesalerProductId",
                table: "OrderItems",
                column: "WholesalerProductId",
                principalTable: "WholesalerProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_WholesalerProducts_WholesalerProductId1",
                table: "OrderItems",
                column: "WholesalerProductId1",
                principalTable: "WholesalerProducts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RetailerProducts_WholesalerProducts_WholesalerProductId",
                table: "RetailerProducts",
                column: "WholesalerProductId",
                principalTable: "WholesalerProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Orders_OrderId",
                table: "Transactions",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Orders_OrderId1",
                table: "Transactions",
                column: "OrderId1",
                principalTable: "Orders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WholesalerProducts_Products_ProductId",
                table: "WholesalerProducts",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_WholesalerProducts_WholesalerProductId",
                table: "CartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_WholesalerProducts_WholesalerProductId1",
                table: "CartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Carts_AspNetUsers_RetailerId",
                table: "Carts");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_WholesalerProducts_WholesalerProductId",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_WholesalerProducts_WholesalerProductId1",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_RetailerProducts_WholesalerProducts_WholesalerProductId",
                table: "RetailerProducts");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Orders_OrderId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Orders_OrderId1",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_WholesalerProducts_Products_ProductId",
                table: "WholesalerProducts");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_OrderId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_OrderId1",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_WholesalerProductId1",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_WholesalerProductId1",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "OrderId1",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "WholesalerProductId1",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "WholesalerProductId1",
                table: "CartItems");

            migrationBuilder.AlterColumn<string>(
                name: "WholesalerId",
                table: "Transactions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "RetailerId",
                table: "Transactions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_OrderId",
                table: "Transactions",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_RetailerId",
                table: "Transactions",
                column: "RetailerId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_WholesalerId",
                table: "Transactions",
                column: "WholesalerId");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_WholesalerProducts_WholesalerProductId",
                table: "CartItems",
                column: "WholesalerProductId",
                principalTable: "WholesalerProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Carts_AspNetUsers_RetailerId",
                table: "Carts",
                column: "RetailerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_WholesalerProducts_WholesalerProductId",
                table: "OrderItems",
                column: "WholesalerProductId",
                principalTable: "WholesalerProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RetailerProducts_WholesalerProducts_WholesalerProductId",
                table: "RetailerProducts",
                column: "WholesalerProductId",
                principalTable: "WholesalerProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_AspNetUsers_RetailerId",
                table: "Transactions",
                column: "RetailerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_AspNetUsers_WholesalerId",
                table: "Transactions",
                column: "WholesalerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Orders_OrderId",
                table: "Transactions",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WholesalerProducts_Products_ProductId",
                table: "WholesalerProducts",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
