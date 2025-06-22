using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DFTRK.ViewModels
{
    public class WholesalerProductCreateViewModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Display(Name = "Product Image")]
        public IFormFile? ImageFile { get; set; }

        [StringLength(255)]
        [Display(Name = "Current Image")]
        public string? ImageUrl { get; set; }

        [Required]
        [Range(0.01, 10000)]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Required]
        [Range(0, 10000)]
        [Display(Name = "Stock Quantity")]
        public int StockQuantity { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        public SelectList? Categories { get; set; }
    }

    public class WholesalerProductEditViewModel
    {
        public int Id { get; set; }
        
        public int ProductId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Display(Name = "Product Image")]
        public IFormFile? ImageFile { get; set; }

        [StringLength(255)]
        [Display(Name = "Current Image")]
        public string? ImageUrl { get; set; }

        [Required]
        [Range(0.01, 10000)]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Required]
        [Range(0, 10000)]
        [Display(Name = "Stock Quantity")]
        public int StockQuantity { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        public SelectList? Categories { get; set; }
    }

    public class WholesalerProductStockViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Product")]
        public string ProductName { get; set; } = string.Empty;

        [Display(Name = "Current Stock")]
        public int CurrentStock { get; set; }

        [Required]
        [Display(Name = "Quantity to Add")]
        public int QuantityToAdd { get; set; }
    }
} 