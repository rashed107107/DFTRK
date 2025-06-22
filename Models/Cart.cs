namespace DFTRK.Models
{
    public class Cart
    {
        public int Id { get; set; }
        
        // Foreign key
        public string RetailerId { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ApplicationUser? Retailer { get; set; }
        public virtual ICollection<CartItem>? Items { get; set; }
    }
} 