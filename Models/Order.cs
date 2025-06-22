using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DFTRK.Models
{
    public class Order
    {
        public int Id { get; set; }
        
        public string RetailerId { get; set; } = string.Empty;
        public ApplicationUser? Retailer { get; set; }
        
        public string WholesalerId { get; set; } = string.Empty;
        public ApplicationUser? Wholesaler { get; set; }
        
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        
        [MaxLength(500)]
        public string? Notes { get; set; }
        
        public ICollection<OrderItem>? Items { get; set; }
        
        public Transaction? Transaction { get; set; }
    }

    public enum OrderStatus
    {
        Pending,
        Processing,
        Shipped,
        Delivered,
        Completed,
        Cancelled
    }
} 