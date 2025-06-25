using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using DFTRK.Models;

namespace DFTRK.ViewModels
{
    public class PartnershipIndexViewModel
    {
        public List<RetailerPartnership> Partnerships { get; set; } = new();
    }

    public class PartnershipCreateViewModel
    {
        [Required]
        [Display(Name = "Partnership Name")]
        [MaxLength(100)]
        public string PartnershipName { get; set; } = string.Empty;

        [Display(Name = "Notes")]
        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    public class PartnershipEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Partnership Name")]
        [MaxLength(100)]
        public string PartnershipName { get; set; } = string.Empty;

        [Display(Name = "Notes")]
        [MaxLength(500)]
        public string? Notes { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }
    }

    public class PartnershipDetailsViewModel
    {
        public RetailerPartnership Partnership { get; set; } = new();
        public List<RetailerPartnerCategory> Categories { get; set; } = new();
        public List<RetailerPartnerProduct> Products { get; set; } = new();
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
    }

    // Category Management ViewModels
    public class CategoryCreateViewModel
    {
        public int PartnershipId { get; set; }

        [Required]
        [Display(Name = "Category Name")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [MaxLength(500)]
        public string? Description { get; set; }

        public string PartnershipName { get; set; } = string.Empty;
    }

    public class CategoryEditViewModel
    {
        public int Id { get; set; }
        public int PartnershipId { get; set; }

        [Required]
        [Display(Name = "Category Name")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [MaxLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        public string PartnershipName { get; set; } = string.Empty;
    }

    // Product Management ViewModels
    public class ProductCreateViewModel
    {
        public int PartnershipId { get; set; }

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required]
        [Display(Name = "Product Name")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Purchase Price")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Purchase price must be greater than 0")]
        public decimal PurchasePrice { get; set; }

        public SelectList? CategoriesList { get; set; }
        public string PartnershipName { get; set; } = string.Empty;
    }

    public class ProductEditViewModel
    {
        public int Id { get; set; }
        public int PartnershipId { get; set; }

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required]
        [Display(Name = "Product Name")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Purchase Price")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Purchase price must be greater than 0")]
        public decimal PurchasePrice { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        public SelectList? CategoriesList { get; set; }
        public string PartnershipName { get; set; } = string.Empty;
    }

    // Product Ordering ViewModels
    public class ProductOrderViewModel
    {
        public RetailerPartnership Partnership { get; set; } = new();
        public List<WholesalerProduct> WholesalerProducts { get; set; } = new();
        public List<OrderItemViewModel> OrderItems { get; set; } = new();
    }

    public class OrderItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal => Price * Quantity;
    }

    // Partner Category Management ViewModels
    public class PartnerCategoryCreateViewModel
    {
        public int PartnershipId { get; set; }

        [Required]
        [Display(Name = "Category Name")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [MaxLength(500)]
        public string? Description { get; set; }

        public string PartnershipName { get; set; } = string.Empty;
    }

    public class PartnerCategoryEditViewModel
    {
        public int Id { get; set; }
        public int PartnershipId { get; set; }

        [Required]
        [Display(Name = "Category Name")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [MaxLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        public string PartnershipName { get; set; } = string.Empty;
    }

    public class PartnerProductCreateViewModel
    {
        public int PartnershipId { get; set; }

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required]
        [Display(Name = "Product Name")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Purchase Price")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Purchase price must be greater than 0")]
        public decimal PurchasePrice { get; set; }

        [Display(Name = "Product Image")]
        public IFormFile? ProductImage { get; set; }

        [Display(Name = "Notes")]
        [MaxLength(500)]
        public string? Notes { get; set; }

        public SelectList? CategoriesList { get; set; }
        public string PartnershipName { get; set; } = string.Empty;
    }

    public class PartnerProductEditViewModel
    {
        public int Id { get; set; }
        public int PartnershipId { get; set; }

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required]
        [Display(Name = "Product Name")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Purchase Price")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Purchase price must be greater than 0")]
        public decimal PurchasePrice { get; set; }

        [Display(Name = "Product Image")]
        public IFormFile? ProductImage { get; set; }

        [Display(Name = "Current Image")]
        public string? CurrentImagePath { get; set; }

        [Display(Name = "Notes")]
        [MaxLength(500)]
        public string? Notes { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        public SelectList? CategoriesList { get; set; }
        public string PartnershipName { get; set; } = string.Empty;
    }

    // Order Management ViewModels
    public class PartnerOrderCreateViewModel
    {
        public int PartnershipId { get; set; }
        public string PartnershipName { get; set; } = string.Empty;
        public string WholesalerName { get; set; } = string.Empty;
        public List<PartnerOrderItemViewModel> OrderItems { get; set; } = new();
        public List<RetailerPartnerProduct> AvailableProducts { get; set; } = new();
        
        [Display(Name = "Order Notes")]
        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    public class PartnerOrderItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal PurchasePrice { get; set; }
        
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
        
        public decimal Subtotal => PurchasePrice * Quantity;
        public bool IsSelected { get; set; }
    }

    public class PartnerOrderDetailsViewModel
    {
        public Order Order { get; set; } = new();
        public RetailerPartnership Partnership { get; set; } = new();
        public List<OrderItem> OrderItems { get; set; } = new();
        public decimal TotalAmount { get; set; }
    }

    public class PartnerOrderIndexViewModel
    {
        public List<Order> Orders { get; set; } = new();
        public int PartnershipId { get; set; }
        public string PartnershipName { get; set; } = string.Empty;
    }
} 