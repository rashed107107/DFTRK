using System.ComponentModel.DataAnnotations;

namespace DFTRK.Models
{
    public class WholesalerExternalRetailer
    {
        public int Id { get; set; }
        
        [Required]
        public string WholesalerId { get; set; } = string.Empty;
        public virtual ApplicationUser? Wholesaler { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string RetailerName { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? ContactInfo { get; set; }
        
        [MaxLength(500)]
        public string? Notes { get; set; }
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
} 