namespace DFTRK.Models
{
    public class RetailerProduct
    {
        public int Id { get; set; }
        
        // Foreign keys
        public int WholesalerProductId { get; set; }
        public string RetailerId { get; set; } = string.Empty;
        
        public decimal PurchasePrice { get; set; }
        public decimal SellingPrice { get; set; }
        public int StockQuantity { get; set; }
        public string? Notes { get; set; }
        
        // Navigation properties
        public virtual WholesalerProduct? WholesalerProduct { get; set; }
        public virtual ApplicationUser? Retailer { get; set; }
    }
} 