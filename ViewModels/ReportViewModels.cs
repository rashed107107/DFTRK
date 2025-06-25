using DFTRK.Models;

namespace DFTRK.ViewModels
{
    // Common data point class for charts
    public class ChartDataPoint
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public int Count { get; set; }
        public string Color { get; set; } = string.Empty;
        public decimal SecondaryValue { get; set; } // For comparing ordered vs paid amounts
    }
    
    // Base report view model with common properties
    public class BaseReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
    
    // Sales report view model
    public class SalesReportViewModel : BaseReportViewModel
    {
        // Filter properties
        public string? RetailerId { get; set; }
        public string? WholesalerId { get; set; }
        public OrderStatus? StatusFilter { get; set; }
        
        // Data collections
        public IEnumerable<Order> Orders { get; set; } = new List<Order>();
        public IEnumerable<ApplicationUser> Retailers { get; set; } = new List<ApplicationUser>();
        public IEnumerable<ApplicationUser> Wholesalers { get; set; } = new List<ApplicationUser>();
        
        // Summary metrics
        public int TotalOrders { get; set; }
        public decimal TotalSales { get; set; }
        public decimal ActualRevenue { get; set; } // Amount actually paid
        public decimal OutstandingAmount { get; set; } // Amount still pending payment
        public decimal CollectionRate { get; set; } // Percentage of total collected
        
        // Payment status counts
        public int FullyPaidOrders { get; set; }
        public int PartiallyPaidOrders { get; set; }
        public int UnpaidOrders { get; set; }
        
        // Order status counts
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        
        // Charts data
        public IEnumerable<ChartDataPoint> OrdersByStatusChart { get; set; } = new List<ChartDataPoint>();
        public IEnumerable<ChartDataPoint> SalesByRetailerChart { get; set; } = new List<ChartDataPoint>();
        public IEnumerable<ChartDataPoint> SalesByWholesalerChart { get; set; } = new List<ChartDataPoint>();
        public IEnumerable<ChartDataPoint> DailySalesChart { get; set; } = new List<ChartDataPoint>();
        
        // Top performers
        public List<TopRetailerItem> TopRetailers { get; set; } = new List<TopRetailerItem>();
        public List<TopWholesalerItem> TopWholesalers { get; set; } = new List<TopWholesalerItem>();
        public List<TopProductItem> TopProducts { get; set; } = new List<TopProductItem>();
    }
    
    // Payments report view model
    public class PaymentsReportViewModel : BaseReportViewModel
    {
        // Filter properties
        public string RetailerId { get; set; }
        public string WholesalerId { get; set; }
        public string PaymentMethodFilter { get; set; }
        
        // Data collections
        public IEnumerable<Payment> Payments { get; set; } = new List<Payment>();
        public IEnumerable<ApplicationUser> Retailers { get; set; } = new List<ApplicationUser>();
        public IEnumerable<ApplicationUser> Wholesalers { get; set; } = new List<ApplicationUser>();
        public IEnumerable<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();
        
        // Summary metrics
        public int TotalPayments { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalPendingAmount { get; set; }
        public decimal CollectionRate { get; set; }
        
        // Charts data
        public IEnumerable<ChartDataPoint> PaymentsByMethodChart { get; set; } = new List<ChartDataPoint>();
        public IEnumerable<ChartDataPoint> PaymentsTrendChart { get; set; } = new List<ChartDataPoint>();
        public IEnumerable<ChartDataPoint> PaymentsByRetailerChart { get; set; } = new List<ChartDataPoint>();
        public IEnumerable<ChartDataPoint> PaymentsByWholesalerChart { get; set; } = new List<ChartDataPoint>();
        
        // Top performers
        public List<TopRetailerItem> TopRetailers { get; set; } = new List<TopRetailerItem>();
        public List<TopWholesalerItem> TopWholesalers { get; set; } = new List<TopWholesalerItem>();
    }
    
    // User report view model
    public class UserReportViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }  // For retailers
        public decimal TotalEarned { get; set; } // For wholesalers
    }

    // Enhanced Retailer performance report view model
    public class RetailerReportViewModel : BaseReportViewModel
    {
        public string RetailerId { get; set; } = string.Empty;
        public string RetailerName { get; set; } = string.Empty;
        
        // Order Metrics
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }
        
        // Financial Metrics
        public decimal TotalOrderValue { get; set; } // Total value of all orders placed
        public decimal ActualAmountSpent { get; set; } // Actual payments made
        public decimal OutstandingBalance { get; set; } // Amount still owed
        public decimal AvgOrderValue { get; set; }
        public decimal PaymentRate => TotalOrderValue > 0 ? (ActualAmountSpent / TotalOrderValue) * 100 : 0; // Percentage paid
        
        // Performance Metrics
        public int TotalProductsPurchased { get; set; }
        public int TotalQuantityOrdered { get; set; }
        public decimal AvgProductPrice { get; set; }
        public int UniqueWholesalers { get; set; }
        public decimal OrderFrequency { get; set; } // Orders per month
        
        // Business Insights
        public string PreferredWholesaler { get; set; } = string.Empty;
        public string MostOrderedCategory { get; set; } = string.Empty;
        public decimal MonthlySpendTrend { get; set; } // Growth rate
        public string PaymentReliability { get; set; } = string.Empty; // Excellent/Good/Poor
        
        // Collections
        public List<OrderWithPaymentStatus> RecentOrders { get; set; } = new List<OrderWithPaymentStatus>();
        public List<TopWholesalerItem> TopWholesalers { get; set; } = new List<TopWholesalerItem>();
        public List<CategorySpendingItem> CategorySpending { get; set; } = new List<CategorySpendingItem>();
        public List<MonthlySpendingTrendItem> MonthlyTrends { get; set; } = new List<MonthlySpendingTrendItem>();
        
        // Chart Data
        public List<ChartDataPoint> PaymentStatusChart { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> OrderStatusChart { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> MonthlySpendingChart { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> CategoryDistributionChart { get; set; } = new List<ChartDataPoint>();
    }

    // Enhanced Wholesaler performance report view model
    public class WholesalerReportViewModel : BaseReportViewModel
    {
        public string WholesalerId { get; set; } = string.Empty;
        public string WholesalerName { get; set; } = string.Empty;
        
        // Order Metrics
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }
        
        // Financial Metrics
        public decimal TotalOrderValue { get; set; } // Total value of all orders received
        public decimal ActualRevenue { get; set; } // Actual payments received
        public decimal OutstandingPayments { get; set; } // Amount still pending
        public decimal AvgOrderValue { get; set; }
        public decimal CollectionRate => TotalOrderValue > 0 ? (ActualRevenue / TotalOrderValue) * 100 : 0; // Percentage collected
        
        // Performance Metrics
        public int TotalProductsSold { get; set; }
        public int TotalQuantitySold { get; set; }
        public decimal AvgSellPrice { get; set; }
        public int UniqueRetailers { get; set; }
        public decimal OrderFrequency { get; set; } // Orders per month
        public int ActiveProducts { get; set; }
        public int LowStockProducts { get; set; }
        
        // Business Insights
        public string TopRetailer { get; set; } = string.Empty;
        public string BestSellingCategory { get; set; } = string.Empty;
        public decimal MonthlyRevenueTrend { get; set; } // Growth rate
        public string InventoryHealth { get; set; } = string.Empty; // Excellent/Good/Poor
        public decimal InventoryTurnover { get; set; }
        
        // Collections
        public List<OrderWithPaymentStatus> RecentOrders { get; set; } = new List<OrderWithPaymentStatus>();
        public List<TopRetailerItem> TopRetailers { get; set; } = new List<TopRetailerItem>();
        public List<TopProductItem> TopProducts { get; set; } = new List<TopProductItem>();
        public List<CategoryPerformanceItem> CategoryPerformance { get; set; } = new List<CategoryPerformanceItem>();
        public List<MonthlyRevenueTrendItem> MonthlyTrends { get; set; } = new List<MonthlyRevenueTrendItem>();
        public List<InventoryStatusItem> InventoryStatus { get; set; } = new List<InventoryStatusItem>();
        
        // Chart Data
        public List<ChartDataPoint> RevenueChart { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> OrderStatusChart { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> PaymentStatusChart { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> CategoryRevenueChart { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> InventoryDistributionChart { get; set; } = new List<ChartDataPoint>();
    }

    // Helper classes for the report view models
    public class OrderWithPaymentStatus
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal RemainingAmount { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public TransactionStatus PaymentStatus { get; set; }
    }

    public class TopWholesalerItem
    {
        public string WholesalerId { get; set; } = string.Empty;
        public string WholesalerName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal ActuallyPaid { get; set; }
    }

    public class TopRetailerItem
    {
        public string RetailerId { get; set; } = string.Empty;
        public string RetailerName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal ActuallyPaid { get; set; }
    }

    public class TopProductItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal SecondaryValue { get; set; } // For dual-axis charts
    }

    // Enhanced helper classes for performance reports
    public class CategorySpendingItem
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalSpent { get; set; }
        public int OrderCount { get; set; }
        public int ProductCount { get; set; }
        public decimal AvgOrderValue { get; set; }
        public decimal SpendingPercentage { get; set; }
    }

    public class MonthlySpendingTrendItem
    {
        public string Month { get; set; } = string.Empty;
        public decimal AmountSpent { get; set; }
        public int OrderCount { get; set; }
        public decimal AvgOrderValue { get; set; }
        public decimal GrowthRate { get; set; }
    }

    public class CategoryPerformanceItem
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int QuantitySold { get; set; }
        public int OrderCount { get; set; }
        public decimal AvgPrice { get; set; }
        public decimal RevenuePercentage { get; set; }
        public string PerformanceTrend { get; set; } = string.Empty; // Up/Down/Stable
    }

    public class MonthlyRevenueTrendItem
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
        public decimal AvgOrderValue { get; set; }
        public decimal GrowthRate { get; set; }
    }

    public class InventoryStatusItem
    {
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public string StockStatus { get; set; } = string.Empty; // Critical/Low/Normal/High
        public decimal StockTurnover { get; set; }
    }

    // Financial report view model for Admin
    public class FinancialReportViewModel : BaseReportViewModel
    {
        // Summary metrics
        public decimal TotalTransactionVolume { get; set; }
        public decimal PlatformFees { get; set; }
        public decimal ActualRevenue { get; set; }
        public decimal CollectionRate { get; set; } // Percentage of total volume collected
        
        // Chart data
        public List<ChartDataPoint> MonthlyTrendChart { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> MonthlyFeesChart { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> PaymentMethodChart { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> AgingChart { get; set; } = new List<ChartDataPoint>();
        
        // Top performers
        public List<TopRetailerFinancialItem> TopRetailersByVolume { get; set; } = new List<TopRetailerFinancialItem>();
        public List<TopWholesalerFinancialItem> TopWholesalersByVolume { get; set; } = new List<TopWholesalerFinancialItem>();
    }
    
    // Financial-specific retailer item
    public class TopRetailerFinancialItem
    {
        public string RetailerId { get; set; } = string.Empty;
        public int TransactionCount { get; set; }
        public decimal TransactionVolume { get; set; }
        public decimal PlatformFees { get; set; }
    }
    
    // Financial-specific wholesaler item
    public class TopWholesalerFinancialItem
    {
        public string WholesalerId { get; set; } = string.Empty;
        public int TransactionCount { get; set; }
        public decimal TransactionVolume { get; set; }
        public decimal PlatformFees { get; set; }
    }

    // Enhanced Retailer Reports ViewModels
    public class RetailerInventoryAnalysisViewModel : BaseReportViewModel
    {
        public List<ProductAnalysisItem> ProductAnalysis { get; set; } = new List<ProductAnalysisItem>();
        public List<CategoryAnalysisItem> CategoryAnalysis { get; set; } = new List<CategoryAnalysisItem>();
        public List<MonthlyTrendItem> MonthlyTrends { get; set; } = new List<MonthlyTrendItem>();
        public List<SupplierAnalysisItem> SupplierAnalysis { get; set; } = new List<SupplierAnalysisItem>();
    }

    public class ProductAnalysisItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalSpent { get; set; }
        public int OrderCount { get; set; }
        public decimal AvgOrderQuantity { get; set; }
        public decimal AvgPrice { get; set; }
        public decimal ReorderFrequency { get; set; } // Days between orders
        public DateTime LastOrderDate { get; set; }
        public string ReorderStatus => ReorderFrequency > 0 ? 
            ReorderFrequency < 30 ? "Frequent" : 
            ReorderFrequency < 60 ? "Regular" : "Infrequent" : "New";
    }

    public class CategoryAnalysisItem
    {
        public string CategoryName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalSpent { get; set; }
        public int ProductCount { get; set; }
        public decimal AvgOrderValue { get; set; }
        public decimal SpendPercentage { get; set; }
    }

    public class MonthlyTrendItem
    {
        public string Month { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalSpent { get; set; }
        public int UniqueProducts { get; set; }
        public string MonthDisplay => DateTime.TryParseExact(Month, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out var date) 
            ? date.ToString("MMM yyyy") : Month;
    }

    public class SupplierAnalysisItem
    {
        public string SupplierId { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public decimal TotalSpent { get; set; }
        public int OrderCount { get; set; }
        public int ProductVariety { get; set; }
        public decimal AvgDeliveryValue { get; set; }
        public decimal ReorderFrequency { get; set; }
        public string ReliabilityRating => 
            ReorderFrequency >= 2 ? "High" :
            ReorderFrequency >= 1 ? "Medium" : "Low";
    }

    // Enhanced Wholesaler Reports ViewModels
    public class WholesalerBusinessInsightsViewModel : BaseReportViewModel
    {
        public List<CustomerAnalysisItem> CustomerAnalysis { get; set; } = new List<CustomerAnalysisItem>();
        public List<WholesalerProductAnalysisItem> ProductAnalysis { get; set; } = new List<WholesalerProductAnalysisItem>();
        public List<SeasonalPatternItem> SeasonalPatterns { get; set; } = new List<SeasonalPatternItem>();
        public List<MarketTrendItem> MarketTrends { get; set; } = new List<MarketTrendItem>();
    }

    public class CustomerAnalysisItem
    {
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AvgOrderValue { get; set; }
        public int CustomerLifespan { get; set; } // Days since first order
        public decimal MonthlyFrequency { get; set; }
        public decimal PaymentReliability { get; set; } // Percentage
        public DateTime LastOrderDate { get; set; }
        public string CustomerTier => 
            TotalRevenue >= 10000 ? "Premium" :
            TotalRevenue >= 5000 ? "Gold" :
            TotalRevenue >= 1000 ? "Silver" : "Bronze";
        public string LifespanDisplay => CustomerLifespan > 365 ? $"{CustomerLifespan / 365} years" : 
            CustomerLifespan > 30 ? $"{CustomerLifespan / 30} months" : $"{CustomerLifespan} days";
    }

    public class WholesalerProductAnalysisItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public decimal CurrentPrice { get; set; }
        public bool IsActive { get; set; }
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public int OrderCount { get; set; }
        public decimal AvgSellPrice { get; set; }
        public decimal StockTurnover { get; set; }
        public int DaysToStockout { get; set; }
        public string PerformanceRating => 
            StockTurnover >= 2 ? "Excellent" :
            StockTurnover >= 1 ? "Good" :
            StockTurnover >= 0.5m ? "Average" : "Poor";
        public string StockStatus => 
            DaysToStockout <= 7 ? "Critical" :
            DaysToStockout <= 30 ? "Low" :
            DaysToStockout <= 90 ? "Normal" : "High";
    }

    public class SeasonalPatternItem
    {
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AvgOrderValue { get; set; }
    }

    public class MarketTrendItem
    {
        public string Category { get; set; } = string.Empty;
        public List<TrendDataPoint> TrendData { get; set; } = new List<TrendDataPoint>();
    }

    public class TrendDataPoint
    {
        public string Period { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Quantity { get; set; }
        public decimal AvgPrice { get; set; }
    }

    // Retailer Profitability Analysis ViewModels
    public class RetailerProfitabilityAnalysisViewModel : BaseReportViewModel
    {
        public List<CategoryProfitabilityItem> CategoryProfitability { get; set; } = new List<CategoryProfitabilityItem>();
        public decimal TotalCosts => CategoryProfitability.Sum(c => c.TotalCost);
        public decimal TotalPotentialRevenue => CategoryProfitability.Sum(c => c.PotentialRevenue);
        public decimal TotalPotentialProfit => TotalPotentialRevenue - TotalCosts;
        public decimal OverallMargin => TotalCosts > 0 ? (TotalPotentialProfit / TotalPotentialRevenue) * 100 : 0;
    }

    public class CategoryProfitabilityItem
    {
        public string Category { get; set; } = string.Empty;
        public decimal TotalCost { get; set; }
        public int TotalQuantity { get; set; }
        public decimal AvgCostPerUnit { get; set; }
        public int ProductCount { get; set; }
        public decimal SuggestedMarkup { get; set; }
        public decimal PotentialRevenue { get; set; }
        public decimal PotentialProfit { get; set; }
        public decimal MarginPercentage => PotentialRevenue > 0 ? (PotentialProfit / PotentialRevenue) * 100 : 0;
        public decimal ROI => TotalCost > 0 ? (PotentialProfit / TotalCost) * 100 : 0;
    }

    // Wholesaler Payment and Revenue Report View Model
    public class WholesalerPaymentsReportViewModel : BaseReportViewModel
    {
        public string WholesalerId { get; set; } = string.Empty;
        public string WholesalerName { get; set; } = string.Empty;
        public string? RetailerId { get; set; }
        public string? PaymentStatus { get; set; }

        // Summary Metrics
        public int TotalOrders { get; set; }
        public decimal TotalOrderValue { get; set; }
        public decimal TotalReceived { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal CollectionRate { get; set; }
        public double AvgDaysToPayment { get; set; }

        // Payment Status Counts
        public int FullyPaidOrders { get; set; }
        public int PartiallyPaidOrders { get; set; }
        public int UnpaidOrders { get; set; }
        public int OverdueOrders { get; set; }
        public decimal OverdueAmount { get; set; }

        // Collections for the view
        public List<Order> Orders { get; set; } = new();
        public List<ApplicationUser> Retailers { get; set; } = new();
        public List<RetailerRevenueItem> RevenueByRetailer { get; set; } = new();
        public List<PaymentMonthlyTrendItem> MonthlyTrends { get; set; } = new();
        public List<PaymentMethodItem> PaymentMethods { get; set; } = new();
        public List<RecentPaymentItem> RecentPayments { get; set; } = new();
        public List<AgingAnalysisItem> AgingAnalysis { get; set; } = new();
    }

    // Helper classes for WholesalerPaymentsReportViewModel
    public class RetailerRevenueItem
    {
        public string RetailerId { get; set; } = string.Empty;
        public string RetailerName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalOrdered { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Outstanding { get; set; }
        public decimal PaymentRate { get; set; }
    }

    public class PaymentMonthlyTrendItem
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalOrdered { get; set; }
        public decimal TotalReceived { get; set; }
    }

    public class PaymentMethodItem
    {
        public string Method { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class RecentPaymentItem
    {
        public Payment Payment { get; set; } = new();
        public Order Order { get; set; } = new();
        public ApplicationUser? Retailer { get; set; }
    }

    public class AgingAnalysisItem
    {
        public string AgeRange { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    // Retailer Purchase Report ViewModels
    public class RetailerPurchaseReportViewModel : BaseReportViewModel
    {
        // Filter properties
        public string? SupplierId { get; set; }
        public string? SupplierType { get; set; } // "Wholesaler" or "Partner"
        
        // Summary metrics
        public int TotalOrders { get; set; }
        public decimal TotalPurchases { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal PaymentRate { get; set; }
        public int TotalItems { get; set; }
        public int WholesalerOrderCount { get; set; }
        public int PartnerOrderCount { get; set; }
        public decimal WholesalerPurchases { get; set; }
        public decimal PartnerPurchases { get; set; }
        
        // Data collections
        public List<SupplierPurchaseSummary> SupplierSummaries { get; set; } = new List<SupplierPurchaseSummary>();
        public List<PurchaseOrderDetail> AllOrders { get; set; } = new List<PurchaseOrderDetail>();
        public List<ApplicationUser> Wholesalers { get; set; } = new List<ApplicationUser>();
        public List<RetailerPartnership> Partners { get; set; } = new List<RetailerPartnership>();
        
        // Charts data
        public List<ChartDataPoint> PurchasesBySupplierChart { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> PurchasesByTypeChart { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> MonthlyPurchasesChart { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> PaymentStatusChart { get; set; } = new List<ChartDataPoint>();
    }

    public class SupplierPurchaseSummary
    {
        public string SupplierId { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierType { get; set; } = string.Empty; // "Wholesaler" or "Partner"
        public int OrderCount { get; set; }
        public decimal TotalPurchases { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Outstanding { get; set; }
        public decimal PaymentRate { get; set; }
        public int TotalItems { get; set; }
        public DateTime LastOrderDate { get; set; }
        public decimal AvgOrderValue { get; set; }
        public List<PurchaseOrderDetail> Orders { get; set; } = new List<PurchaseOrderDetail>();
    }

    public class PurchaseOrderDetail
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string SupplierId { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierType { get; set; } = string.Empty; // "Wholesaler" or "Partner"
        public OrderStatus Status { get; set; }
        public decimal OrderTotal { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Outstanding { get; set; }
        public TransactionStatus PaymentStatus { get; set; }
        public int ItemCount { get; set; }
        public List<PurchaseItemDetail> Items { get; set; } = new List<PurchaseItemDetail>();
        public List<PaymentDetail> Payments { get; set; } = new List<PaymentDetail>();
    }

    public class PurchaseItemDetail
    {
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
        public bool IsPartnershipProduct { get; set; }
    }

    public class PaymentDetail
    {
        public int PaymentId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public string Reference { get; set; } = string.Empty;
    }
} 