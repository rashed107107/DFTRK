namespace DFTRK.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        
        // Foreign keys
        public int OrderId { get; set; }
        public int? WholesalerProductId { get; set; }
        
        // Partnership product information (for when WholesalerProductId is null)
        public int? PartnerProductId { get; set; }
        public string? ProductName { get; set; }
        
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
        
        // Navigation properties
        public virtual Order? Order { get; set; }
        public virtual WholesalerProduct? WholesalerProduct { get; set; }
    }
} 