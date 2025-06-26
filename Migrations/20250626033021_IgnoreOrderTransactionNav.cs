using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DFTRK.Migrations
{
    /// <inheritdoc />
    public partial class IgnoreOrderTransactionNav : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Orders_OrderId1",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_OrderId1",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "OrderId1",
                table: "Transactions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderId1",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_OrderId1",
                table: "Transactions",
                column: "OrderId1",
                unique: true,
                filter: "[OrderId1] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Orders_OrderId1",
                table: "Transactions",
                column: "OrderId1",
                principalTable: "Orders",
                principalColumn: "Id");
        }
    }
}
