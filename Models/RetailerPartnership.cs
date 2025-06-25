using System.ComponentModel.DataAnnotations;

namespace DFTRK.Models
{
    public class RetailerPartnership
    {
        public int Id { get; set; }
        
        [Required]
        public string RetailerId { get; set; } = string.Empty;
        public virtual ApplicationUser? Retailer { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string WholesalerName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string PartnershipName { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Notes { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual ICollection<RetailerPartnerCategory>? Categories { get; set; }
        public virtual ICollection<RetailerPartnerProduct>? Products { get; set; }
    }
} 