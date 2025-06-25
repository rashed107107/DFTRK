using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DFTRK.Models
{
    public class RetailerPartnerProduct
    {
        public int Id { get; set; }
        
        [Required]
        public int PartnershipId { get; set; }
        public virtual RetailerPartnership? Partnership { get; set; }
        
        [Required]
        public int CategoryId { get; set; }
        public virtual RetailerPartnerCategory? Category { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        [MaxLength(50)]
        public string? SKU { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal SellingPrice { get; set; }
        
        public int StockQuantity { get; set; } = 0;
        public int MinimumStock { get; set; } = 0;
        
        [MaxLength(500)]
        public string? ImageUrl { get; set; }
        
        [MaxLength(1000)]
        public string? Notes { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdated { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Calculated properties
        [NotMapped]
        public decimal ProfitMargin => CostPrice > 0 ? ((SellingPrice - CostPrice) / CostPrice) * 100 : 0;
        
        [NotMapped]
        public decimal ProfitAmount => SellingPrice - CostPrice;
        
        [NotMapped]
        public bool IsLowStock => StockQuantity <= MinimumStock;
    }
} 