using DFTRK.Models;

namespace DFTRK.ViewModels
{
    public class AdminDashboardViewModel
    {
        // Basic metrics
        public int TotalUsers { get; set; }
        public int TotalWholesalers { get; set; }
        public int TotalRetailers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public IEnumerable<Order> RecentOrders { get; set; } = new List<Order>();

        // Enhanced financial metrics
        public decimal ActualRevenue { get; set; } // Actual payments received
        public decimal OutstandingRevenue { get; set; } // Amount still pending
        public decimal CollectionRate { get; set; } // Payment collection rate percentage
        public decimal PlatformFees { get; set; } // 0.1% of transaction volume
        public decimal NetRevenue { get; set; } // Actual revenue minus platform fees

        // Order status breakdown
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int CompletedOrders { get; set; }

        // Time-based metrics
        public int TodaysOrders { get; set; }
        public decimal TodaysRevenue { get; set; }
        public decimal TodaysPlatformFees { get; set; }
        public int MonthlyOrders { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public decimal MonthlyPlatformFees { get; set; }

        // Product metrics
        public int TotalWholesalerProducts { get; set; }
        public int TotalRetailerProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }

        // Analytics
        public int TotalPayments { get; set; }
        public decimal AvgOrderValue { get; set; }

        // Top performers
        public IEnumerable<dynamic> TopWholesalers { get; set; } = new List<dynamic>();
        public IEnumerable<dynamic> TopRetailers { get; set; } = new List<dynamic>();
    }

    public class WholesalerDashboardViewModel
    {
        // Basic Metrics
        public int TotalProducts { get; set; }
        public int TotalPendingOrders { get; set; }
        public int TotalCompletedOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int LowStockCount { get; set; }
        public IEnumerable<Order> RecentOrders { get; set; } = new List<Order>();

        // Enhanced Business Intelligence
        public decimal ActualRevenue { get; set; } // Actual payments received
        public decimal OutstandingPayments { get; set; } // Amount still pending
        public decimal OutstandingRevenue { get; set; } // Amount still pending (alternative name)
        public decimal CollectionRate => TotalRevenue > 0 ? (ActualRevenue / TotalRevenue) * 100 : 0;
        public int ActiveProducts { get; set; }
        public int CriticalStockProducts { get; set; } // Stock < 10
        public int UniqueRetailers { get; set; }
        public decimal AvgOrderValue { get; set; } // Made settable

        // Additional required properties
        public int TotalStock { get; set; }
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int OutOfStockCount { get; set; }
        
        // Monthly Performance
        public decimal MonthlyRevenue { get; set; }
        public decimal LastMonthRevenue { get; set; }
        public decimal RevenueGrowth => LastMonthRevenue > 0 ? ((MonthlyRevenue - LastMonthRevenue) / LastMonthRevenue) * 100 : 0;
        public int MonthlyOrders { get; set; }
        public int LastMonthOrders { get; set; }
        public decimal OrderGrowth => LastMonthOrders > 0 ? ((MonthlyOrders - LastMonthOrders) / (decimal)LastMonthOrders) * 100 : 0;

        // Quick Stats
        public int TodaysOrders { get; set; }
        public decimal TodaysRevenue { get; set; }
        public int ThisWeekOrders { get; set; }
        public decimal ThisWeekRevenue { get; set; }

        // Business Insights
        public string TopRetailer { get; set; } = string.Empty;
        public string BestSellingProduct { get; set; } = string.Empty;
        public string MostOrderedCategory { get; set; } = string.Empty;
        
        // Charts and Analytics
        public List<ChartDataPoint> RevenueChart { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> OrderStatusChart { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> TopProductsChart { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> MonthlyTrendsChart { get; set; } = new List<ChartDataPoint>();
        
        // Alert System
        public List<DashboardAlert> Alerts { get; set; } = new List<DashboardAlert>();
        
        // Top Performers
        public List<TopRetailerSummary> TopRetailers { get; set; } = new List<TopRetailerSummary>();
        public List<ProductPerformanceSummary> TopProducts { get; set; } = new List<ProductPerformanceSummary>();
        
        // Inventory Health
        public decimal InventoryTurnover { get; set; }
        public string InventoryHealth { get; set; } = string.Empty; // Excellent/Good/Poor
        public List<InventoryAlert> InventoryAlerts { get; set; } = new List<InventoryAlert>();
    }

    public class RetailerDashboardViewModel
    {
        // Enhanced inventory statistics
        public int UniqueProductCount { get; set; }
        public int TotalProductQuantity { get; set; }
        
        // Legacy property (kept for backward compatibility)
        public int TotalProductsInInventory { get; set; }
        
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public IEnumerable<Order> RecentOrders { get; set; } = new List<Order>();
        
        // Inventory statistics
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public decimal InventoryValue { get; set; }
        public decimal PotentialSalesValue { get; set; }
        public IEnumerable<RetailerProduct> TopProducts { get; set; } = new List<RetailerProduct>();

        // Enhanced Business Intelligence
        public decimal ActualAmountSpent { get; set; } // Actual payments made
        public decimal OutstandingBalance { get; set; } // Amount still owed
        public decimal PaymentRate => TotalSpent > 0 ? (ActualAmountSpent / TotalSpent) * 100 : 0;
        public decimal PotentialProfit => PotentialSalesValue - InventoryValue;
        public decimal ProfitMargin => InventoryValue > 0 ? (PotentialProfit / PotentialSalesValue) * 100 : 0;
        
        // Order Performance
        public int CompletedOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int ShippedOrders { get; set; }
        public decimal AvgOrderValue => TotalOrders > 0 ? TotalSpent / TotalOrders : 0;
        
        // Monthly Performance
        public decimal MonthlySpending { get; set; }
        public decimal LastMonthSpending { get; set; }
        public decimal SpendingGrowth => LastMonthSpending > 0 ? ((MonthlySpending - LastMonthSpending) / LastMonthSpending) * 100 : 0;
        public int MonthlyOrders { get; set; }
        public int LastMonthOrders { get; set; }
        public decimal OrderFrequency => MonthlyOrders > 0 ? MonthlyOrders / 4.33m : 0; // Orders per week
        
        // Quick Stats
        public int TodaysOrders { get; set; }
        public decimal TodaysSpending { get; set; }
        public int ThisWeekOrders { get; set; }
        public decimal ThisWeekSpending { get; set; }
        
        // Business Insights
        public string PreferredWholesaler { get; set; } = string.Empty;
        public string MostOrderedCategory { get; set; } = string.Empty;
        public string PaymentReliability { get; set; } = string.Empty; // Excellent/Good/Poor
        
        // Charts and Analytics
        public List<ChartDataPoint> SpendingChart { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> InventoryChart { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> CategoryChart { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> PaymentStatusChart { get; set; } = new List<ChartDataPoint>();
        
        // Alert System
        public List<DashboardAlert> Alerts { get; set; } = new List<DashboardAlert>();
        
        // Top Performers
        public List<TopWholesalerSummary> TopWholesalers { get; set; } = new List<TopWholesalerSummary>();
        public List<CategorySpendingSummary> TopCategories { get; set; } = new List<CategorySpendingSummary>();
        
        // Reorder Recommendations
        public List<ReorderRecommendation> ReorderRecommendations { get; set; } = new List<ReorderRecommendation>();
    }

    // Enhanced Helper Classes

    public class DashboardAlert
    {
        public string Type { get; set; } = string.Empty; // danger, warning, info, success
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string ActionUrl { get; set; } = string.Empty;
        public string ActionText { get; set; } = string.Empty;
        public int Priority { get; set; } // 1 = high, 2 = medium, 3 = low
        public DateTime Created { get; set; } = DateTime.UtcNow;
    }

    public class TopRetailerSummary
    {
        public string RetailerId { get; set; } = string.Empty;
        public string RetailerName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal ActualRevenue { get; set; }
        public decimal PaymentRate { get; set; } // Made settable
        public DateTime LastOrderDate { get; set; }
        public string Status { get; set; } = string.Empty; // Active, Inactive, New
    }

    public class TopWholesalerSummary
    {
        public string WholesalerId { get; set; } = string.Empty;
        public string WholesalerName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal ActualSpent { get; set; }
        public DateTime LastOrderDate { get; set; }
        public decimal AvgOrderValue => OrderCount > 0 ? TotalSpent / OrderCount : 0;
        public string Status { get; set; } = string.Empty;
    }

    public class ProductPerformanceSummary
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
        public int CurrentStock { get; set; }
        public string StockStatus { get; set; } = string.Empty; // Critical/Low/Normal/High
        public decimal Velocity { get; set; } // Sales per week
    }

    public class CategorySpendingSummary
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalSpent { get; set; }
        public int OrderCount { get; set; }
        public int ProductCount { get; set; }
        public decimal AvgOrderValue => OrderCount > 0 ? TotalSpent / OrderCount : 0;
        public decimal SpendingPercentage { get; set; }
    }

    public class InventoryAlert
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public string AlertType { get; set; } = string.Empty; // OutOfStock, LowStock, Overstocked
        public string Message { get; set; } = string.Empty;
        public int RecommendedAction { get; set; } // Reorder quantity
    }

    public class ReorderRecommendation
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int RecommendedQuantity { get; set; }
        public decimal EstimatedCost { get; set; }
        public string Urgency { get; set; } = string.Empty; // Critical, High, Medium, Low
        public string Reason { get; set; } = string.Empty;
        public int DaysUntilStockout { get; set; }
        public decimal WeeklySales { get; set; }
    }
} 