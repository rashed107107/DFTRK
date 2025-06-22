using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace DFTRK.Models
{
    public enum UserType
    {
        Admin,
        Wholesaler,
        Retailer
    }

    public class ApplicationUser : IdentityUser
    {
        public string? BusinessName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? TaxId { get; set; }
        public UserType UserType { get; set; }
        
        // Navigation properties - using NotMapped to avoid EF Core relationship conflicts
        [NotMapped]
        public virtual ICollection<Order>? Orders { get; set; }
        
        [NotMapped]
        public virtual ICollection<Transaction>? Transactions { get; set; }
        
        [NotMapped]
        public virtual ICollection<WholesalerProduct>? WholesalerProducts { get; set; }
        
        [NotMapped]
        public virtual ICollection<RetailerProduct>? RetailerProducts { get; set; }
    }
} 