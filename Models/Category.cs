using System.ComponentModel.DataAnnotations;

namespace DFTRK.Models
{
    public class Category
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;
        
        // Foreign key for creator
        public string? CreatedById { get; set; }
        
        // Navigation property for creator
        public ApplicationUser? CreatedBy { get; set; }
        
        // Navigation property for products
        public ICollection<Product>? Products { get; set; }
    }
} 