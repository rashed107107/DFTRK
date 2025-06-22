namespace DFTRK.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        
        // Foreign keys
        public int CartId { get; set; }
        public int WholesalerProductId { get; set; }
        
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        
        // Navigation properties
        public virtual Cart? Cart { get; set; }
        public virtual WholesalerProduct? WholesalerProduct { get; set; }
    }
} 