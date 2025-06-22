using System.ComponentModel.DataAnnotations;

namespace DFTRK.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        // Making SKU optional
        [StringLength(50)]
        public string? SKU { get; set; }

        [Required]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        [StringLength(255)]
        public string? ImageUrl { get; set; }

        // Navigation properties
        public ICollection<WholesalerProduct>? WholesalerProducts { get; set; }
    }
} 