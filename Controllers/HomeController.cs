using System.Diagnostics;
using DFTRK.Data;
using DFTRK.Models;
using DFTRK.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace DFTRK.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public HomeController(
            ILogger<HomeController> logger, 
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _logger = logger;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await InitializeCartCountAsync(user.Id);
                    
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                    {
                        return RedirectToAction("AdminDashboard");
                    }
                    else if (await _userManager.IsInRoleAsync(user, "Wholesaler"))
                    {
                        return RedirectToAction("WholesalerDashboard");
                    }
                    else if (await _userManager.IsInRoleAsync(user, "Retailer"))
                    {
                        return RedirectToAction("RetailerDashboard");
                    }
                }
            }
            
            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            try
            {
            // Get counts for dashboard
            var totalUsers = await _userManager.Users.CountAsync();
            var totalWholesalers = await _userManager.GetUsersInRoleAsync("Wholesaler").ConfigureAwait(false);
            var totalRetailers = await _userManager.GetUsersInRoleAsync("Retailer").ConfigureAwait(false);
            var totalProducts = await _context.Products.CountAsync();
            var totalOrders = await _context.Orders.CountAsync();
                
                // Enhanced revenue calculation - use same logic as reports (exclude cancelled orders)
                var allTransactions = await _context.Transactions
                    .Include(t => t.Order)
                    .Include(t => t.Payments)
                    .Where(t => t.Order.Status != OrderStatus.Cancelled) // Exclude cancelled orders
                    .ToListAsync();

                // Update transaction amounts to ensure consistency
                foreach (var transaction in allTransactions)
                {
                    var actualAmountPaid = transaction.Payments?.Sum(p => p.Amount) ?? 0;
                    if (transaction.AmountPaid != actualAmountPaid)
                    {
                        transaction.AmountPaid = actualAmountPaid;
                        _context.Update(transaction);
                    }
                }
                await _context.SaveChangesAsync();

                // Calculate metrics using same logic as reports
                var totalRevenue = allTransactions.Sum(t => t.Amount); // Total transaction volume
                var actualRevenue = allTransactions.Sum(t => t.Payments?.Sum(p => p.Amount) ?? 0); // Actual payments received
                var outstandingRevenue = totalRevenue - actualRevenue;

                // Calculate platform fees (0.1% of transaction volume)
                var platformFees = totalRevenue * 0.001m;
                var netRevenue = actualRevenue - platformFees;

                // Order status breakdown
                var pendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
                var processingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Processing);
                var shippedOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Shipped);
                var completedOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered);

                // Time-based analytics
                var now = DateTime.UtcNow;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var startOfToday = now.Date;
                
                // Today's metrics (using consistent logic)
                var todaysTransactions = allTransactions.Where(t => t.Order.OrderDate >= startOfToday).ToList();
                var todaysOrders = todaysTransactions.Count();
                var todaysRevenue = todaysTransactions.Sum(t => t.Payments?.Sum(p => p.Amount) ?? 0);
                var todaysPlatformFees = todaysTransactions.Sum(t => t.Amount) * 0.001m;

                // Monthly metrics (using consistent logic)
                var monthlyTransactions = allTransactions.Where(t => t.Order.OrderDate >= startOfMonth).ToList();
                var monthlyOrders = monthlyTransactions.Count();
                var monthlyRevenue = monthlyTransactions.Sum(t => t.Payments?.Sum(p => p.Amount) ?? 0);
                var monthlyPlatformFees = monthlyTransactions.Sum(t => t.Amount) * 0.001m;

                // Product analytics
                var totalWholesalerProducts = await _context.WholesalerProducts.CountAsync();
                var totalRetailerProducts = await _context.RetailerProducts.CountAsync();
                var lowStockProducts = await _context.WholesalerProducts.CountAsync(wp => wp.StockQuantity > 0 && wp.StockQuantity <= 5);
                var outOfStockProducts = await _context.WholesalerProducts.CountAsync(wp => wp.StockQuantity == 0);

                // Payment analytics
                var totalPayments = await _context.Payments.CountAsync();
                var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;
                var collectionRate = totalRevenue > 0 ? (actualRevenue / totalRevenue) * 100 : 0;

                // Get recent orders with better includes
            var recentOrders = await _context.Orders
                .Include(o => o.Retailer)
                .Include(o => o.Wholesaler)
                    .Include(o => o.Transaction)
                    .Where(o => o.Status != OrderStatus.Cancelled) // Exclude cancelled orders
                .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                .ToListAsync();

                // Top performing wholesalers and retailers (using actual revenue)
                var topWholesalers = allTransactions
                    .GroupBy(t => new { t.WholesalerId, t.Order.Wholesaler!.BusinessName })
                    .Select(g => new { 
                        Name = g.Key.BusinessName, 
                        Revenue = g.Sum(t => t.Payments?.Sum(p => p.Amount) ?? 0),
                        OrderCount = g.Count()
                    })
                    .OrderByDescending(x => x.Revenue)
                    .Take(5)
                    .ToList();

                var topRetailers = allTransactions
                    .GroupBy(t => new { t.RetailerId, t.Order.Retailer!.BusinessName })
                    .Select(g => new { 
                        Name = g.Key.BusinessName, 
                        Spent = g.Sum(t => t.Payments?.Sum(p => p.Amount) ?? 0),
                        OrderCount = g.Count()
                    })
                    .OrderByDescending(x => x.Spent)
                    .Take(5)
                    .ToList();

                // Create enhanced view model
            var viewModel = new AdminDashboardViewModel
            {
                    // Basic metrics
                TotalUsers = totalUsers,
                TotalWholesalers = totalWholesalers.Count,
                TotalRetailers = totalRetailers.Count,
                TotalProducts = totalProducts,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                    RecentOrders = recentOrders,

                    // Enhanced financial metrics
                    ActualRevenue = actualRevenue,
                    OutstandingRevenue = outstandingRevenue,
                    CollectionRate = collectionRate,
                    PlatformFees = platformFees,
                    NetRevenue = netRevenue,
                    
                    // Order status breakdown
                    PendingOrders = pendingOrders,
                    ProcessingOrders = processingOrders,
                    ShippedOrders = shippedOrders,
                    CompletedOrders = completedOrders,
                    
                    // Time-based metrics
                    TodaysOrders = todaysOrders,
                    TodaysRevenue = todaysRevenue,
                    TodaysPlatformFees = todaysPlatformFees,
                    MonthlyOrders = monthlyOrders,
                    MonthlyRevenue = monthlyRevenue,
                    MonthlyPlatformFees = monthlyPlatformFees,
                    
                    // Product metrics
                    TotalWholesalerProducts = totalWholesalerProducts,
                    TotalRetailerProducts = totalRetailerProducts,
                    LowStockProducts = lowStockProducts,
                    OutOfStockProducts = outOfStockProducts,
                    
                    // Analytics
                    TotalPayments = totalPayments,
                    AvgOrderValue = avgOrderValue,
                    
                    // Top performers
                    TopWholesalers = topWholesalers,
                    TopRetailers = topRetailers
            };

            return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AdminDashboard");
                
                // Return basic model in case of error
                var basicModel = new AdminDashboardViewModel
                {
                    TotalUsers = 0,
                    TotalWholesalers = 0,
                    TotalRetailers = 0,
                    TotalProducts = 0,
                    TotalOrders = 0,
                    TotalRevenue = 0,
                    RecentOrders = new List<Order>()
                };
                
                return View(basicModel);
            }
        }

        [Authorize(Roles = "Wholesaler")]
        public async Task<IActionResult> WholesalerDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            try
            {
                // Get all orders for this wholesaler
                var allOrders = await _context.Orders
                    .Include(o => o.Items)
                    .ThenInclude(oi => oi.WholesalerProduct)
                    .ThenInclude(wp => wp.Product)
                    .ThenInclude(p => p.Category)
                .Include(o => o.Retailer)
                .Where(o => o.WholesalerId == user.Id)
                    .ToListAsync();

                // Get all transactions for this wholesaler
                var allTransactions = await _context.Transactions
                    .Where(t => t.WholesalerId == user.Id)
                    .ToListAsync();

                // Get wholesaler products
                var wholesalerProducts = await _context.WholesalerProducts
                    .Include(wp => wp.Product)
                    .ThenInclude(p => p.Category)
                    .Where(wp => wp.WholesalerId == user.Id)
                    .ToListAsync();

                // Calculate date ranges
                var now = DateTime.UtcNow;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var startOfWeek = now.AddDays(-(int)now.DayOfWeek);
                var startOfToday = now.Date;

                // Order status calculations
                var completedOrders = allOrders.Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered).ToList();
                var pendingOrders = allOrders.Where(o => o.Status == OrderStatus.Pending).ToList();
                var processingOrders = allOrders.Where(o => o.Status == OrderStatus.Processing).ToList();
                var shippedOrders = allOrders.Where(o => o.Status == OrderStatus.Shipped).ToList();

                // Financial calculations
                var totalRevenue = completedOrders.Sum(o => o.TotalAmount);
                var actualRevenue = allTransactions.Sum(t => t.AmountPaid);
                var outstandingRevenue = totalRevenue - actualRevenue;

                // Time-based analytics
                var monthlyOrders = allOrders.Where(o => o.OrderDate >= startOfMonth).ToList();
                var weeklyOrders = allOrders.Where(o => o.OrderDate >= startOfWeek).ToList();
                var todaysOrders = allOrders.Where(o => o.OrderDate >= startOfToday).ToList();

                var monthlyRevenue = monthlyOrders.Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount);
                var weeklyRevenue = weeklyOrders.Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount);
                var todaysRevenue = todaysOrders.Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount);

                // Product analytics
                var totalProducts = wholesalerProducts.Count;
                var totalStock = wholesalerProducts.Sum(wp => wp.StockQuantity);
                var lowStockProducts = wholesalerProducts.Count(wp => wp.StockQuantity > 0 && wp.StockQuantity <= 5);
                var outOfStockProducts = wholesalerProducts.Count(wp => wp.StockQuantity == 0);

                // Top products
                var topProducts = allOrders
                    .SelectMany(o => o.Items)
                    .Where(oi => oi.WholesalerProduct?.Product != null)
                    .GroupBy(oi => new { 
                        oi.WholesalerProduct.ProductId, 
                        ProductName = oi.WholesalerProduct.Product.Name, 
                        CategoryName = oi.WholesalerProduct.Product.Category?.Name 
                    })
                    .Select(g => new ProductPerformanceSummary
                    {
                        ProductName = g.Key.ProductName,
                        CategoryName = g.Key.CategoryName ?? "Uncategorized",
                        QuantitySold = g.Sum(oi => oi.Quantity),
                        Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice),
                        CurrentStock = wholesalerProducts.FirstOrDefault(wp => wp.ProductId == g.Key.ProductId)?.StockQuantity ?? 0
                    })
                    .OrderByDescending(p => p.Revenue)
                    .Take(5)
                    .ToList();

                // Top retailers
                var topRetailers = allOrders
                    .Where(o => o.Retailer != null)
                    .GroupBy(o => new { o.RetailerId, o.Retailer.BusinessName })
                    .Select(g => new TopRetailerSummary
                    {
                        RetailerId = g.Key.RetailerId,
                        RetailerName = g.Key.BusinessName,
                        OrderCount = g.Count(),
                        TotalRevenue = g.Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount),
                        LastOrderDate = g.Max(o => o.OrderDate),
                        PaymentRate = 0 // We'll calculate this properly if needed
                    })
                    .OrderByDescending(r => r.TotalRevenue)
                    .Take(5)
                    .ToList();

                // Recent orders
                var recentOrders = allOrders
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                    .ToList();

            var viewModel = new WholesalerDashboardViewModel
            {
                    // Basic metrics
                TotalProducts = totalProducts,
                    TotalStock = totalStock,
                    TotalOrders = allOrders.Count,
                TotalRevenue = totalRevenue,
                    ActualRevenue = actualRevenue,
                    OutstandingRevenue = outstandingRevenue,

                    // Order performance
                    CompletedOrders = completedOrders.Count,
                    PendingOrders = pendingOrders.Count,
                    ProcessingOrders = processingOrders.Count,
                    ShippedOrders = shippedOrders.Count,

                    // Time-based performance
                    MonthlyRevenue = monthlyRevenue,
                    MonthlyOrders = monthlyOrders.Count,
                    TodaysOrders = todaysOrders.Count,
                    TodaysRevenue = todaysRevenue,
                    ThisWeekOrders = weeklyOrders.Count,
                    ThisWeekRevenue = weeklyRevenue,

                    // Product analytics
                    LowStockCount = lowStockProducts,
                    OutOfStockCount = outOfStockProducts,
                    AvgOrderValue = allOrders.Count > 0 ? totalRevenue / allOrders.Count : 0,

                    // Top performers
                    TopProducts = topProducts,
                    TopRetailers = topRetailers,
                    RecentOrders = recentOrders,

                    // Charts - keeping empty for now since we simplified
                    RevenueChart = new List<ChartDataPoint>()
            };

            return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WholesalerDashboard");
                
                var basicModel = new WholesalerDashboardViewModel
                {
                    TotalProducts = 0,
                    TotalStock = 0,
                    TotalOrders = 0,
                    TotalRevenue = 0,
                    ActualRevenue = 0,
                    OutstandingRevenue = 0,
                    TopProducts = new List<ProductPerformanceSummary>(),
                    TopRetailers = new List<TopRetailerSummary>(),
                    RecentOrders = new List<Order>()
                };
                
                return View(basicModel);
            }
        }

        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> RetailerDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            await InitializeCartCountAsync(user.Id);

            try
            {
                // Get comprehensive data with all necessary navigation properties
                var retailerProducts = await _context.RetailerProducts
                    .Include(rp => rp.WholesalerProduct)
                    .ThenInclude(wp => wp.Product)
                    .ThenInclude(p => p.Category)
                    .Include(rp => rp.WholesalerProduct)
                    .ThenInclude(wp => wp.Wholesaler)
                    .Where(rp => rp.RetailerId == user.Id)
                    .ToListAsync();

                var allOrders = await _context.Orders
                    .Include(o => o.Items)
                    .ThenInclude(oi => oi.WholesalerProduct)
                    .ThenInclude(wp => wp.Product)
                    .ThenInclude(p => p.Category)
                    .Include(o => o.Wholesaler)
                    .Where(o => o.RetailerId == user.Id)
                    .ToListAsync();

                var allTransactions = await _context.Transactions
                    .Where(t => t.RetailerId == user.Id)
                    .ToListAsync();

                // Calculate date ranges
                var now = DateTime.UtcNow;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var startOfLastMonth = startOfMonth.AddMonths(-1);
                var startOfWeek = now.AddDays(-(int)now.DayOfWeek);
                var startOfToday = now.Date;

                // Order status calculations
                var completedOrders = allOrders.Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered).ToList();
                var pendingOrders = allOrders.Where(o => o.Status == OrderStatus.Pending).ToList();
                var processingOrders = allOrders.Where(o => o.Status == OrderStatus.Processing).ToList();
                var shippedOrders = allOrders.Where(o => o.Status == OrderStatus.Shipped).ToList();

                // Financial calculations (using actual transactions for accurate data)
                var totalSpent = completedOrders.Sum(o => o.TotalAmount);
                var actualAmountSpent = allTransactions.Sum(t => t.AmountPaid);
                var outstandingBalance = totalSpent - actualAmountSpent;

                // Time-based analytics
                var monthlyOrders = allOrders.Where(o => o.OrderDate >= startOfMonth).ToList();
                var lastMonthOrders = allOrders.Where(o => o.OrderDate >= startOfLastMonth && o.OrderDate < startOfMonth).ToList();
                var weeklyOrders = allOrders.Where(o => o.OrderDate >= startOfWeek).ToList();
                var todaysOrders = allOrders.Where(o => o.OrderDate >= startOfToday).ToList();

                var monthlySpending = monthlyOrders.Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount);
                var lastMonthSpending = lastMonthOrders.Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount);
                var weeklySpending = weeklyOrders.Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount);
                var todaysSpending = todaysOrders.Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount);

                // Inventory calculations
                var groupedProducts = retailerProducts
                    .GroupBy(rp => rp.WholesalerProductId)
                    .Select(group => new {
                        WholesalerProductId = group.Key,
                        WholesalerProduct = group.First().WholesalerProduct,
                        TotalStock = group.Sum(rp => rp.StockQuantity),
                        TotalValue = group.Sum(rp => rp.PurchasePrice * rp.StockQuantity),
                        TotalPotentialValue = group.Sum(rp => rp.SellingPrice * rp.StockQuantity)
                    }).ToList();

                var uniqueProductCount = groupedProducts.Count;
                var totalStockQuantity = groupedProducts.Sum(p => p.TotalStock);
                var inventoryValue = groupedProducts.Sum(p => p.TotalValue);
                var potentialSalesValue = groupedProducts.Sum(p => p.TotalPotentialValue);
                var lowStockProducts = groupedProducts.Count(p => p.TotalStock > 0 && p.TotalStock <= 5);
                var outOfStockProducts = groupedProducts.Count(p => p.TotalStock == 0);

                // Business insights
                var preferredWholesaler = allOrders
                    .GroupBy(o => new { o.WholesalerId, o.Wholesaler?.BusinessName })
                    .OrderByDescending(g => g.Sum(o => o.TotalAmount))
                    .FirstOrDefault()?.Key.BusinessName ?? "None";

                var mostOrderedCategory = allOrders
                    .SelectMany(o => o.Items)
                    .Where(oi => oi.WholesalerProduct?.Product?.Category != null)
                    .GroupBy(oi => oi.WholesalerProduct.Product.Category.Name)
                    .OrderByDescending(g => g.Sum(oi => oi.Quantity))
                    .FirstOrDefault()?.Key ?? "None";

                // Payment reliability calculation
                var paymentReliability = actualAmountSpent > 0 && totalSpent > 0 
                    ? (actualAmountSpent / totalSpent) >= 0.9m ? "Excellent" 
                      : (actualAmountSpent / totalSpent) >= 0.7m ? "Good" 
                      : "Needs Improvement"
                    : "No Data";

                // Chart data preparation
                var spendingChart = new List<ChartDataPoint>();
                var inventoryChart = new List<ChartDataPoint>();
                var categoryChart = new List<ChartDataPoint>();
                var paymentStatusChart = new List<ChartDataPoint>();

                // Generate 7-Day Spending Trend - Daily spending for last 7 days
                for (int i = 6; i >= 0; i--)
                {
                    var date = DateTime.Now.Date.AddDays(-i);
                    var dayOrdersValue = allOrders
                        .Where(o => o.OrderDate.Date == date && 
                                   (o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered))
                        .Sum(o => o.TotalAmount);
                    
                    spendingChart.Add(new ChartDataPoint
                    {
                        Label = date.ToString("MMM dd"),
                        Value = dayOrdersValue,
                        Count = allOrders.Count(o => o.OrderDate.Date == date)
                    });
                }

                // Generate Payment Status Distribution with actual payment data
                var paymentStatusGroups = allTransactions
                    .GroupBy(t => t.Status.ToString())
                    .Select(g => new { Status = g.Key, Amount = g.Sum(t => t.AmountPaid), Count = g.Count() })
                    .ToList();

                var statusColors = new Dictionary<string, string>
                {
                    { "Completed", "#28a745" },
                    { "Pending", "#ffc107" },
                    { "PartiallyPaid", "#17a2b8" },
                    { "Failed", "#dc3545" },
                    { "Refunded", "#6c757d" }
                };

                foreach (var group in paymentStatusGroups)
                {
                    paymentStatusChart.Add(new ChartDataPoint
                    {
                        Label = group.Status == "Completed" ? "Paid" : 
                               group.Status == "PartiallyPaid" ? "Partially Paid" : group.Status,
                        Value = group.Amount,
                        Count = group.Count,
                        Color = statusColors.ContainsKey(group.Status) ? statusColors[group.Status] : "#6c757d"
                    });
                }

                // If no payment data, show order-based status
                if (!paymentStatusChart.Any())
                {
                    var orderStatusGroups = new[]
                    {
                        new { Status = "Completed", Amount = completedOrders.Sum(o => o.TotalAmount), Count = completedOrders.Count, Color = "#28a745" },
                        new { Status = "Pending", Amount = pendingOrders.Sum(o => o.TotalAmount), Count = pendingOrders.Count, Color = "#ffc107" },
                        new { Status = "Processing", Amount = processingOrders.Sum(o => o.TotalAmount), Count = processingOrders.Count, Color = "#17a2b8" },
                        new { Status = "Shipped", Amount = shippedOrders.Sum(o => o.TotalAmount), Count = shippedOrders.Count, Color = "#007bff" }
                    }.Where(g => g.Count > 0);

                    foreach (var group in orderStatusGroups)
                    {
                        paymentStatusChart.Add(new ChartDataPoint
                        {
                            Label = group.Status,
                            Value = group.Amount,
                            Count = group.Count,
                            Color = group.Color
                        });
                    }
                }

                // Generate Inventory Health Chart
                if (uniqueProductCount > 0)
                {
                    var inStockCount = uniqueProductCount - lowStockProducts - outOfStockProducts;
                    inventoryChart.Add(new ChartDataPoint { Label = "In Stock", Value = inStockCount, Color = "#28a745" });
                    if (lowStockProducts > 0)
                        inventoryChart.Add(new ChartDataPoint { Label = "Low Stock", Value = lowStockProducts, Color = "#ffc107" });
                    if (outOfStockProducts > 0)
                        inventoryChart.Add(new ChartDataPoint { Label = "Out of Stock", Value = outOfStockProducts, Color = "#dc3545" });
                }

                // Alert system
                var alerts = new List<DashboardAlert>();

                // Critical inventory alerts
                if (outOfStockProducts > 0)
                {
                    alerts.Add(new DashboardAlert
                    {
                        Type = "danger",
                        Icon = "bi-exclamation-triangle-fill",
                        Title = "Out of Stock Alert",
                        Message = $"You have {outOfStockProducts} product(s) out of stock",
                        ActionUrl = "/RetailerProducts",
                        ActionText = "Manage Inventory",
                        Priority = 1
                    });
                }

                if (lowStockProducts > 0)
                {
                    alerts.Add(new DashboardAlert
                    {
                        Type = "warning",
                        Icon = "bi-exclamation-circle-fill",
                        Title = "Low Stock Warning",
                        Message = $"{lowStockProducts} product(s) running low on stock",
                        ActionUrl = "/RetailerProducts",
                        ActionText = "Review Inventory",
                        Priority = 2
                    });
                }

                if (outstandingBalance > 100)
                {
                    alerts.Add(new DashboardAlert
                    {
                        Type = "info",
                        Icon = "bi-credit-card",
                        Title = "Outstanding Payments",
                        Message = $"You have ${outstandingBalance:N2} in outstanding payments",
                        ActionUrl = "/Payments",
                        ActionText = "Make Payment",
                        Priority = 2
                    });
                }

                // Top wholesalers analysis
                var topWholesalers = allOrders
                    .Where(o => o.Wholesaler != null)
                    .GroupBy(o => new { o.WholesalerId, o.Wholesaler.BusinessName })
                    .Select(g => new TopWholesalerSummary
                    {
                        WholesalerId = g.Key.WholesalerId,
                        WholesalerName = g.Key.BusinessName,
                        OrderCount = g.Count(),
                        TotalSpent = g.Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount),
                        ActualSpent = allTransactions.Where(t => t.Order != null && t.Order.WholesalerId == g.Key.WholesalerId).Sum(t => t.AmountPaid),
                        LastOrderDate = g.Max(o => o.OrderDate),
                        Status = g.Any(o => o.OrderDate >= startOfMonth) ? "Active" : "Inactive"
                    })
                    .OrderByDescending(w => w.TotalSpent)
                    .Take(5)
                    .ToList();

                // Category spending analysis
                var topCategories = allOrders
                    .SelectMany(o => o.Items)
                    .Where(oi => oi.WholesalerProduct?.Product?.Category != null)
                    .GroupBy(oi => oi.WholesalerProduct.Product.Category.Name)
                    .Select(g => new CategorySpendingSummary
                    {
                        CategoryName = g.Key,
                        TotalSpent = g.Sum(oi => oi.Quantity * oi.UnitPrice),
                        OrderCount = g.Select(oi => oi.OrderId).Distinct().Count(),
                        ProductCount = g.Select(oi => oi.WholesalerProduct.ProductId).Distinct().Count()
                    })
                    .OrderByDescending(c => c.TotalSpent)
                    .Take(5)
                    .ToList();

                // Calculate spending percentages
                var totalCategorySpending = topCategories.Sum(c => c.TotalSpent);
                foreach (var category in topCategories)
                {
                    category.SpendingPercentage = totalCategorySpending > 0 ? (category.TotalSpent / totalCategorySpending) * 100 : 0;
                }

                // Generate Category Spending Chart
                categoryChart = topCategories.Take(5).Select(c => new ChartDataPoint
                {
                    Label = c.CategoryName,
                    Value = c.TotalSpent,
                    Count = c.OrderCount
                }).ToList();

                // Reorder recommendations - temporarily disabled due to compilation issues
                var reorderRecommendations = new List<ReorderRecommendation>();

                var viewModel = new RetailerDashboardViewModel
                {
                    // Basic metrics
                    UniqueProductCount = uniqueProductCount,
                    TotalProductQuantity = totalStockQuantity,
                    TotalProductsInInventory = uniqueProductCount,
                    TotalOrders = allOrders.Count,
                    TotalSpent = totalSpent,
                    RecentOrders = allOrders.OrderByDescending(o => o.OrderDate).Take(5).ToList(),

                    // Inventory metrics
                    LowStockCount = lowStockProducts,
                    OutOfStockCount = outOfStockProducts,
                    InventoryValue = inventoryValue,
                    PotentialSalesValue = potentialSalesValue,
                    
                    // Enhanced business intelligence
                    ActualAmountSpent = actualAmountSpent,
                    OutstandingBalance = outstandingBalance,

                    // Order performance
                    CompletedOrders = completedOrders.Count,
                    PendingOrders = pendingOrders.Count,
                    ProcessingOrders = processingOrders.Count,
                    ShippedOrders = shippedOrders.Count,

                    // Time-based performance
                    MonthlySpending = monthlySpending,
                    LastMonthSpending = lastMonthSpending,
                    MonthlyOrders = monthlyOrders.Count,
                    LastMonthOrders = lastMonthOrders.Count,
                    TodaysOrders = todaysOrders.Count,
                    TodaysSpending = todaysSpending,
                    ThisWeekOrders = weeklyOrders.Count,
                    ThisWeekSpending = weeklySpending,

                    // Business insights
                    PreferredWholesaler = preferredWholesaler,
                    MostOrderedCategory = mostOrderedCategory,
                    PaymentReliability = paymentReliability,

                    // Charts and analytics
                    SpendingChart = spendingChart,
                    InventoryChart = inventoryChart,
                    CategoryChart = categoryChart,
                    PaymentStatusChart = paymentStatusChart,

                    // Alert system
                    Alerts = alerts.OrderBy(a => a.Priority).ToList(),

                    // Top performers
                    TopWholesalers = topWholesalers,
                    TopCategories = topCategories,

                    // Reorder recommendations
                    ReorderRecommendations = reorderRecommendations,

                    // Top products (legacy)
                    TopProducts = groupedProducts
                        .Where(p => p.WholesalerProduct?.Product != null)
                        .OrderByDescending(p => p.TotalStock)
                        .Take(5)
                        .Select(g => 
                        {
                            var firstProduct = retailerProducts.First(rp => rp.WholesalerProductId == g.WholesalerProductId);
                            firstProduct.StockQuantity = g.TotalStock; 
                            return firstProduct;
                        })
                        .ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RetailerDashboard");
                
                var basicModel = new RetailerDashboardViewModel
                {
                    UniqueProductCount = 0,
                    TotalProductQuantity = 0,
                    TotalProductsInInventory = 0,
                    TotalOrders = 0,
                    TotalSpent = 0,
                    RecentOrders = new List<Order>(),
                    LowStockCount = 0,
                    OutOfStockCount = 0,
                    InventoryValue = 0,
                    PotentialSalesValue = 0,
                    TopProducts = new List<RetailerProduct>()
                };
                
                return View(basicModel);
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Helper method to set cart count in ViewBag
        private async Task InitializeCartCountAsync(string userId)
        {
            if (User.IsInRole("Retailer"))
            {
                var cartItemCount = await _context.Carts
                    .Where(c => c.RetailerId == userId)
                    .SelectMany(c => c.Items)
                    .CountAsync();

                ViewBag.CartItemCount = cartItemCount;
            }
        }
    }
}
