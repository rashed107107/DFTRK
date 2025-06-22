using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DFTRK.Models
{
    public enum PaymentMethod
    {
        CreditCard,
        BankTransfer,
        Cash,
        Other
    }
    
    public class Payment
    {
        public int Id { get; set; }
        
        // Foreign keys
        public int TransactionId { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        
        public PaymentMethod Method { get; set; } = PaymentMethod.CreditCard;
        
        [MaxLength(100)]
        public string? ReferenceNumber { get; set; }
        
        [MaxLength(500)]
        public string? Notes { get; set; }
        
        // Navigation properties
        public virtual Transaction? Transaction { get; set; }
    }
} 