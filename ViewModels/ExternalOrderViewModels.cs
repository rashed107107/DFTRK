using DFTRK.Models;
using System.ComponentModel.DataAnnotations;

namespace DFTRK.ViewModels
{
    public class ExternalOrderViewModel
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Outstanding { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
    }

    public class CreateExternalOrderViewModel
    {
        [Required]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;

        [Display(Name = "Customer Contact")]
        public string? CustomerContact { get; set; }

        public List<ProductSelectionItem> AvailableProducts { get; set; } = new();
        public List<ProductSelectionItem> SelectedProducts { get; set; } = new();
    }

    public class ProductSelectionItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int AvailableStock { get; set; }
        public int Quantity { get; set; }
    }

    public class ExternalPaymentViewModel
    {
        public int OrderId { get; set; }
        public int TransactionId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Outstanding { get; set; }

        [Required]
        [Display(Name = "Payment Amount")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than 0")]
        public decimal PaymentAmount { get; set; }

        [Required]
        [Display(Name = "Payment Method")]
        public PaymentMethod PaymentMethod { get; set; }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }
    }
} 