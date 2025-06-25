using System.ComponentModel.DataAnnotations;

namespace DFTRK.Models
{
    public class RetailerPartnerCategory
    {
        public int Id { get; set; }
        
        [Required]
        public int PartnershipId { get; set; }
        public virtual RetailerPartnership? Partnership { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual ICollection<RetailerPartnerProduct>? Products { get; set; }
    }
} 