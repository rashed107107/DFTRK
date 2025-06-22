using DFTRK.Models;
using System.ComponentModel.DataAnnotations;

namespace DFTRK.ViewModels
{
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "SKU")]
        public string? SKU { get; set; }

        [Display(Name = "Product Image")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Current Image")]
        public string? ImageUrl { get; set; }

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }
    }

    public class WholesalerProductViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Product")]
        public int ProductId { get; set; }

        [Required]
        [Range(0.01, 9999999.99)]
        [DataType(DataType.Currency)]
        [Display(Name = "Price")]
        public decimal Price { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        [Display(Name = "Stock Quantity")]
        public int StockQuantity { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    public class RetailerProductDetailsViewModel
    {
        public WholesalerProduct? WholesalerProduct { get; set; }
        public bool IsInCart { get; set; }
        
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public class ProductCatalogViewModel
    {
        public List<WholesalerProduct>? WholesalerProducts { get; set; }
        public List<Category>? Categories { get; set; }
        public int? CategoryId { get; set; }
        public string? SearchTerm { get; set; }
    }
} 