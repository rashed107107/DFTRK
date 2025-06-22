namespace DFTRK.Models
{
    public class WholesalerProduct
    {
        public int Id { get; set; }
        
        // Foreign keys
        public int ProductId { get; set; }
        public string WholesalerId { get; set; } = string.Empty;
        
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual Product? Product { get; set; }
        public virtual ApplicationUser? Wholesaler { get; set; }
        public virtual ICollection<CartItem>? CartItems { get; set; }
        public virtual ICollection<OrderItem>? OrderItems { get; set; }
    }
} 