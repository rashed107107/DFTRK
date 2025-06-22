using DFTRK.Models;
using System.ComponentModel.DataAnnotations;

namespace DFTRK.ViewModels
{
    public class PaymentViewModel
    {
        public int TransactionId { get; set; }
        public int OrderId { get; set; }
        public string OrderReference { get; set; } = string.Empty;
        public string WholesalerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal RemainingAmount { get; set; }
        public TransactionStatus Status { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public IEnumerable<PaymentItemViewModel>? PaymentHistory { get; set; }
    }
    
    public class PaymentItemViewModel
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public PaymentMethod Method { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
    }
    
    public class MakePaymentViewModel
    {
        public int TransactionId { get; set; }
        public int OrderId { get; set; }
        public string OrderReference { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal RemainingAmount { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than 0.")]
        [Display(Name = "Payment Amount")]
        public decimal PaymentAmount { get; set; }
        
        [Required]
        [Display(Name = "Payment Method")]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CreditCard;
        
        [Display(Name = "Reference Number")]
        [StringLength(100)]
        public string? ReferenceNumber { get; set; }
        
        [Display(Name = "Notes")]
        [StringLength(500)]
        public string? Notes { get; set; }
        
        public bool IsFullPayment { get; set; } = true;
    }
} 