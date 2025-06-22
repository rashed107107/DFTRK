using System.ComponentModel.DataAnnotations;
using DFTRK.Models;

namespace DFTRK.ViewModels
{
    public class RetailerProductEditViewModel
    {
        public int Id { get; set; }
        
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;
        
        [Display(Name = "Purchase Price")]
        [DataType(DataType.Currency)]
        public decimal PurchasePrice { get; set; }
        
        [Required]
        [Range(0.01, 9999999.99)]
        [Display(Name = "Selling Price")]
        [DataType(DataType.Currency)]
        public decimal SellingPrice { get; set; }
        
        [Required]
        [Range(0, int.MaxValue)]
        [Display(Name = "Stock Quantity")]
        public int StockQuantity { get; set; }
    }

    public class RetailerProductViewModel
    {
        public int Id { get; set; }
        public WholesalerProduct? WholesalerProduct { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SellingPrice { get; set; }
        public int StockQuantity { get; set; }
        public string? Notes { get; set; }

        public decimal ProfitMargin => 
            PurchasePrice > 0 ? Math.Round((SellingPrice - PurchasePrice) / PurchasePrice * 100, 2) : 0;
    }
} 