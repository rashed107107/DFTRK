using DFTRK.Models;

namespace DFTRK.ViewModels
{
    public class OrdersViewModel
    {
        public List<OrderViewModel> Orders { get; set; } = new List<OrderViewModel>();
        public bool IsRetailer { get; set; }
        public bool IsWholesaler { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class OrderViewModel
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string RetailerName { get; set; } = string.Empty;
        public string WholesalerName { get; set; } = string.Empty;
        public TransactionStatus TransactionStatus { get; set; }
    }
} 