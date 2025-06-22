using System.ComponentModel.DataAnnotations.Schema;

namespace DFTRK.Models
{
    public enum TransactionStatus
    {
        Pending,
        PartiallyPaid,
        Completed,
        Failed,
        Refunded
    }

    public class Transaction
    {
        public int Id { get; set; }
        
        // Foreign keys
        public int OrderId { get; set; }
        public string RetailerId { get; set; } = string.Empty;
        public string WholesalerId { get; set; } = string.Empty;
        
        public decimal Amount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; } = 0;
        
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
        public string? PaymentMethod { get; set; }
        public string? TransactionReference { get; set; }
        
        // Navigation properties
        public virtual Order? Order { get; set; }
        
        [NotMapped]
        public virtual ApplicationUser? Retailer { get; set; }
        
        [NotMapped]
        public virtual ApplicationUser? Wholesaler { get; set; }
        
        public virtual ICollection<Payment>? Payments { get; set; }
        
        [NotMapped]
        public decimal RemainingAmount => Amount - AmountPaid;
        
        [NotMapped]
        public bool IsFullyPaid => AmountPaid >= Amount;
    }
} 