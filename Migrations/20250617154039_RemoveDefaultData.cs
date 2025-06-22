using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DFTRK.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDefaultData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete all cart items first
            migrationBuilder.Sql("DELETE FROM CartItems");
            
            // Delete all order items
            migrationBuilder.Sql("DELETE FROM OrderItems");
            
            // Delete all transactions
            migrationBuilder.Sql("DELETE FROM Transactions");
            
            // Delete all orders
            migrationBuilder.Sql("DELETE FROM Orders");
            
            // Delete all retailer products
            migrationBuilder.Sql("DELETE FROM RetailerProducts");
            
            // Delete all wholesaler products
            migrationBuilder.Sql("DELETE FROM WholesalerProducts");
            
            // Delete all products
            migrationBuilder.Sql("DELETE FROM Products");
            
            // Delete all categories
            migrationBuilder.Sql("DELETE FROM Categories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No down migration provided as we can't restore the deleted data
        }
    }
} 