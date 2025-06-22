using DFTRK.Data;
using DFTRK.Models;
using DFTRK.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DFTRK.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Reports
        public IActionResult Index()
        {
            return View();
        }

        // GET: Reports/WholesalerIndex - Reports dashboard for Wholesalers
        [Authorize(Roles = "Wholesaler")]
        public IActionResult WholesalerIndex()
        {
            return View();
        }

        // GET: Reports/RetailerIndex - Reports dashboard for Retailers
        [Authorize(Roles = "Retailer")]
        public IActionResult RetailerIndex()
        {
            return View();
        }

        // GET: Reports/Financial - Financial performance report for Admin
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Financial(DateTime? startDate, DateTime? endDate)
        {
            // Default to current month if no dates provided
            startDate ??= new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            endDate ??= DateTime.UtcNow.AddDays(1).AddSeconds(-1); // Include all of the end date

            // Get all transactions within the date range (exclude cancelled orders to match other reports)
            var transactions = await _context.Transactions
                .Include(t => t.Order)
                .Include(t => t.Payments)
                .Where(t => t.Order.OrderDate >= startDate && t.Order.OrderDate <= endDate &&
                          t.Order.Status != OrderStatus.Cancelled) // Match Sales and Payments reports
                .ToListAsync();

            // Update transaction amounts to ensure consistency with other reports
            foreach (var transaction in transactions)
            {
                var actualAmountPaid = transaction.Payments?.Sum(p => p.Amount) ?? 0;
                if (transaction.AmountPaid != actualAmountPaid)
                {
                    transaction.AmountPaid = actualAmountPaid;
                    _context.Update(transaction);
                }
            }
            await _context.SaveChangesAsync();
            
            // Calculate platform fees (0.001 or 0.1% of transaction amount)
            decimal totalTransactionVolume = transactions.Sum(t => t.Amount);
            decimal platformFees = totalTransactionVolume * 0.001m;
            decimal actualRevenue = transactions.Sum(t => t.Payments?.Sum(p => p.Amount) ?? 0); // Use actual payments like other reports
            
            // Calculate monthly breakdown for trend analysis (use Order.OrderDate for consistency)
            var monthlyData = transactions
                .GroupBy(t => new { Month = t.Order.OrderDate.Month, Year = t.Order.OrderDate.Year })
                .Select(g => new
                {
                    Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                    TransactionCount = g.Count(),
                    Volume = g.Sum(t => t.Amount),
                    Collected = g.Sum(t => t.Payments?.Sum(p => p.Amount) ?? 0), // Use actual payments
                    PlatformFees = g.Sum(t => t.Amount) * 0.001m
                })
                .OrderBy(x => x.Period)
                .ToList();

            // Top retailers by transaction volume
            var topRetailers = transactions
                .GroupBy(t => t.RetailerId)
                .Select(g => new
                {
                    RetailerId = g.Key,
                    TransactionCount = g.Count(),
                    Volume = g.Sum(t => t.Amount),
                    Fees = g.Sum(t => t.Amount) * 0.001m
                })
                .OrderByDescending(r => r.Volume)
                .Take(10)
                .ToList();

            // Top wholesalers by transaction volume
            var topWholesalers = transactions
                .GroupBy(t => t.WholesalerId)
                .Select(g => new
                {
                    WholesalerId = g.Key,
                    TransactionCount = g.Count(),
                    Volume = g.Sum(t => t.Amount),
                    Fees = g.Sum(t => t.Amount) * 0.001m
                })
                .OrderByDescending(w => w.Volume)
                .Take(10)
                .ToList();

            // Calculate payment method distribution (use Order.OrderDate for consistency with other reports)
            var paymentMethods = await _context.Payments
                .Include(p => p.Transaction)
                    .ThenInclude(t => t.Order)
                .Where(p => p.Transaction.Order.OrderDate >= startDate && p.Transaction.Order.OrderDate <= endDate &&
                          p.Transaction.Order.Status != OrderStatus.Cancelled) // Exclude cancelled orders
                .GroupBy(p => p.Method)
                .Select(g => new
                {
                    Method = g.Key.ToString(),
                    Count = g.Count(),
                    Amount = g.Sum(p => p.Amount)
                })
                .ToListAsync();

            // Calculate accounts receivable aging (exclude cancelled orders for consistency)
            var outstandingTransactions = await _context.Transactions
                .Include(t => t.Order)
                .Include(t => t.Payments)
                .Where(t => t.Status != TransactionStatus.Completed &&
                          t.Order.Status != OrderStatus.Cancelled) // Exclude cancelled orders
                .ToListAsync();

            var agingBuckets = new Dictionary<string, decimal>
            {
                { "0-30 days", 0 },
                { "31-60 days", 0 },
                { "61-90 days", 0 },
                { "90+ days", 0 }
            };

            foreach (var transaction in outstandingTransactions)
            {
                var daysOutstanding = (DateTime.UtcNow - transaction.Order.OrderDate).Days; // Use Order.OrderDate for consistency
                var outstandingAmount = transaction.Amount - (transaction.Payments?.Sum(p => p.Amount) ?? 0); // Use actual payments

                if (daysOutstanding <= 30)
                    agingBuckets["0-30 days"] += outstandingAmount;
                else if (daysOutstanding <= 60)
                    agingBuckets["31-60 days"] += outstandingAmount;
                else if (daysOutstanding <= 90)
                    agingBuckets["61-90 days"] += outstandingAmount;
                else
                    agingBuckets["90+ days"] += outstandingAmount;
            }

            // Build the view model
            var viewModel = new FinancialReportViewModel
            {
                StartDate = startDate.Value,
                EndDate = endDate.Value,
                TotalTransactionVolume = totalTransactionVolume,
                PlatformFees = platformFees,
                ActualRevenue = actualRevenue,
                CollectionRate = totalTransactionVolume > 0 ? (actualRevenue / totalTransactionVolume) * 100 : 0,
                
                MonthlyTrendChart = monthlyData.Select(m => new ChartDataPoint
                {
                    Label = m.Period,
                    Value = m.Volume,
                    Count = m.TransactionCount
                }).ToList(),
                
                MonthlyFeesChart = monthlyData.Select(m => new ChartDataPoint
                {
                    Label = m.Period,
                    Value = m.PlatformFees,
                    Count = m.TransactionCount
                }).ToList(),
                
                PaymentMethodChart = paymentMethods.Select(p => new ChartDataPoint
                {
                    Label = p.Method,
                    Value = p.Amount,
                    Count = p.Count
                }).ToList(),
                
                AgingChart = agingBuckets.Select(a => new ChartDataPoint
                {
                    Label = a.Key,
                    Value = a.Value
                }).ToList(),
                
                TopRetailersByVolume = topRetailers.Select(r => new TopRetailerFinancialItem
                {
                    RetailerId = r.RetailerId,
                    TransactionCount = r.TransactionCount,
                    TransactionVolume = r.Volume,
                    PlatformFees = r.Fees
                }).ToList(),
                
                TopWholesalersByVolume = topWholesalers.Select(w => new TopWholesalerFinancialItem
                {
                    WholesalerId = w.WholesalerId,
                    TransactionCount = w.TransactionCount,
                    TransactionVolume = w.Volume,
                    PlatformFees = w.Fees
                }).ToList()
            };

            return View(viewModel);
        }

        // GET: Reports/Sales - Enhanced sales reports for Admin
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Sales(DateTime? startDate, DateTime? endDate, string retailerId = null, string wholesalerId = null, string status = null)
        {
            // Default to current month if no dates provided
            startDate ??= new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            endDate ??= DateTime.UtcNow.AddDays(1).AddSeconds(-1); // Include all of the end date

            // Parse status filter if provided
            OrderStatus? statusFilter = null;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var parsedStatus))
            {
                statusFilter = parsedStatus;
            }

            // Get all retailers and wholesalers for filter dropdowns
            var retailers = await _userManager.GetUsersInRoleAsync("Retailer");
            var wholesalers = await _userManager.GetUsersInRoleAsync("Wholesaler");

            // Start building query with all necessary includes
            var query = _context.Orders
                .Include(o => o.Retailer)
                .Include(o => o.Wholesaler)
                .Include(o => o.Transaction)
                    .ThenInclude(t => t.Payments)
                .Include(o => o.Items)
                    .ThenInclude(i => i.WholesalerProduct)
                        .ThenInclude(wp => wp.Product)
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .Where(o => o.Status != OrderStatus.Cancelled); // Exclude cancelled orders from payment calculations

            // Apply filters if provided
            if (!string.IsNullOrEmpty(retailerId))
            {
                query = query.Where(o => o.RetailerId == retailerId);
            }

            if (!string.IsNullOrEmpty(wholesalerId))
            {
                query = query.Where(o => o.WholesalerId == wholesalerId);
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(o => o.Status == statusFilter.Value);
            }

            // Execute query
            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Reload transactions to ensure we have the latest data
            var orderIds = orders.Select(o => o.Id).ToList();
            var transactions = await _context.Transactions
                .Include(t => t.Payments)
                .Where(t => orderIds.Contains(t.OrderId))
                .ToListAsync();
                
            // Create lookup dictionary for quick access - handle potential duplicates
            var transactionLookup = transactions
                .GroupBy(t => t.OrderId)
                .ToDictionary(g => g.Key, g => g.First());

            // Fix transaction statuses in the database
            bool hasChanges = false;
            foreach (var transaction in transactions)
            {
                // Calculate the actual amount paid from the Payments collection
                decimal actualAmountPaid = transaction.Payments?.Sum(p => p.Amount) ?? 0;
                
                // Update the AmountPaid field if it's incorrect
                if (transaction.AmountPaid != actualAmountPaid)
                {
                    transaction.AmountPaid = actualAmountPaid;
                    hasChanges = true;
                }
                
                // Update transaction status based on actual payment
                if (actualAmountPaid >= transaction.Amount && transaction.Status != TransactionStatus.Completed)
                {
                    transaction.Status = TransactionStatus.Completed;
                    _context.Update(transaction);
                    hasChanges = true;
                }
                else if (actualAmountPaid > 0 && actualAmountPaid < transaction.Amount && 
                        transaction.Status == TransactionStatus.Pending)
                {
                    transaction.Status = TransactionStatus.PartiallyPaid;
                    _context.Update(transaction);
                    hasChanges = true;
                }
            }
            
            // Save any status changes to the database
            if (hasChanges)
            {
                await _context.SaveChangesAsync();
                
                // Reload transactions after saving changes
                transactions = await _context.Transactions
                    .Include(t => t.Payments)
                    .Where(t => orderIds.Contains(t.OrderId))
                    .ToListAsync();
                    
                // Update the lookup dictionary
                transactionLookup = transactions.GroupBy(t => t.OrderId).ToDictionary(g => g.Key, g => g.First());
            }

            // Calculate revenue metrics using transactions for consistency with other reports
            decimal totalOrderValue = transactions.Sum(t => t.Amount); // Use Transaction.Amount instead of Order.TotalAmount
            
            // Calculate actual revenue from payments (consistent with Payments and Financial reports)
            decimal actualRevenue = transactions.Sum(t => t.Payments?.Sum(p => p.Amount) ?? 0);
            
            // Calculate outstanding amount (both from same source now)
            decimal outstandingAmount = totalOrderValue - actualRevenue;

            // Group orders by status for chart data
            var ordersByStatus = orders
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    Status = g.Key.ToString(),
                    Count = g.Count(),
                    Value = g.Sum(o => o.TotalAmount)
                })
                .ToList();

            // Group by retailer for chart
            var salesByRetailer = orders
                .GroupBy(o => o.RetailerId)
                .Select(g => new
                {
                    RetailerId = g.Key,
                    RetailerName = g.First().Retailer?.BusinessName ?? "Unknown",
                    OrderCount = g.Count(),
                    TotalAmount = g.Sum(o => o.TotalAmount),
                    ActuallyPaid = g.Where(o => transactionLookup.ContainsKey(o.Id))
                                    .Sum(o => transactionLookup[o.Id].Payments?.Sum(p => p.Amount) ?? 0)
                })
                .OrderByDescending(r => r.TotalAmount)
                .Take(10)
                .ToList();

            // Group by wholesaler for chart
            var salesByWholesaler = orders
                .GroupBy(o => o.WholesalerId)
                .Select(g => new
                {
                    WholesalerId = g.Key,
                    WholesalerName = g.First().Wholesaler?.BusinessName ?? "Unknown",
                    OrderCount = g.Count(),
                    TotalAmount = g.Sum(o => o.TotalAmount),
                    ActuallyPaid = g.Where(o => transactionLookup.ContainsKey(o.Id))
                                    .Sum(o => transactionLookup[o.Id].Payments?.Sum(p => p.Amount) ?? 0)
                })
                .OrderByDescending(w => w.TotalAmount)
                .Take(10)
                .ToList();

            // Group by day for daily sales chart
            var dailySales = orders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count(),
                    Value = g.Sum(o => o.TotalAmount),
                    ActuallyPaid = g.Where(o => transactionLookup.ContainsKey(o.Id))
                                    .Sum(o => transactionLookup[o.Id].Payments?.Sum(p => p.Amount) ?? 0)
                })
                .OrderBy(d => d.Date)
                .ToList();

            // Get top products
            var topProducts = orders
                .SelectMany(o => o.Items ?? new List<OrderItem>())
                .GroupBy(i => new { 
                    ProductId = i.WholesalerProduct?.Product?.Id ?? 0, 
                    Name = i.WholesalerProduct?.Product?.Name ?? "Unknown" 
                })
                .Select(g => new TopProductItem
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    QuantitySold = g.Sum(i => i.Quantity),
                    TotalRevenue = g.Sum(i => i.Subtotal)
                })
                .OrderByDescending(p => p.TotalRevenue)
                .Take(10)
                .ToList();

            // Calculate order status counts
            var pendingOrders = orders.Count(o => o.Status == OrderStatus.Pending);
            var processingOrders = orders.Count(o => o.Status == OrderStatus.Processing);
            var shippedOrders = orders.Count(o => o.Status == OrderStatus.Shipped);
            var deliveredOrders = orders.Count(o => o.Status == OrderStatus.Delivered);
            var completedOrders = orders.Count(o => o.Status == OrderStatus.Completed);
            var cancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled);

            // Calculate payment status metrics
            var fullyPaidOrders = orders.Count(o => o.Transaction != null && o.Transaction.AmountPaid >= o.Transaction.Amount);
            var partiallyPaidOrders = orders.Count(o => o.Transaction != null && o.Transaction.AmountPaid > 0 && o.Transaction.AmountPaid < o.Transaction.Amount);
            var unpaidOrders = orders.Count(o => o.Transaction == null || o.Transaction.AmountPaid == 0);
            
            // Payment collection rate
            decimal collectionRate = totalOrderValue > 0 ? (actualRevenue / totalOrderValue) * 100 : 0;

            // Build the enhanced view model
            var viewModel = new SalesReportViewModel
            {
                // Dates and filters
                StartDate = startDate.Value,
                EndDate = endDate.Value,
                RetailerId = retailerId,
                WholesalerId = wholesalerId,
                StatusFilter = statusFilter,
                
                // Collections for dropdowns
                Orders = orders.Select(o => {
                    // Update Transaction information with the latest data from our lookup
                    if (o.Transaction == null && transactionLookup.ContainsKey(o.Id))
                    {
                        o.Transaction = transactionLookup[o.Id];
                    }
                    else if (o.Transaction != null && transactionLookup.ContainsKey(o.Id))
                    {
                        // Make sure we have the latest payment data
                        o.Transaction.AmountPaid = transactionLookup[o.Id].AmountPaid;
                        o.Transaction.Status = transactionLookup[o.Id].Status;
                        o.Transaction.Payments = transactionLookup[o.Id].Payments;
                    }
                    return o;
                }).ToList(),
                Retailers = retailers,
                Wholesalers = wholesalers,
                
                // Summary metrics
                TotalOrders = orders.Count,
                TotalSales = totalOrderValue,
                ActualRevenue = actualRevenue,
                OutstandingAmount = outstandingAmount,
                CollectionRate = collectionRate,
                
                // Payment status counts
                FullyPaidOrders = fullyPaidOrders,
                PartiallyPaidOrders = partiallyPaidOrders,
                UnpaidOrders = unpaidOrders,
                
                // Order status counts
                PendingOrders = pendingOrders,
                ProcessingOrders = processingOrders,
                ShippedOrders = shippedOrders,
                DeliveredOrders = deliveredOrders,
                CompletedOrders = completedOrders,
                CancelledOrders = cancelledOrders,
                
                // Chart data
                OrdersByStatusChart = ordersByStatus.Select(o => new ChartDataPoint
                {
                    Label = o.Status,
                    Value = o.Value,
                    Count = o.Count
                }).ToList(),
                
                SalesByRetailerChart = salesByRetailer.Select(r => new ChartDataPoint
                {
                    Label = r.RetailerName,
                    Value = r.TotalAmount,
                    Count = r.OrderCount,
                    SecondaryValue = r.ActuallyPaid
                }).ToList(),
                
                SalesByWholesalerChart = salesByWholesaler.Select(w => new ChartDataPoint
                {
                    Label = w.WholesalerName,
                    Value = w.TotalAmount,
                    Count = w.OrderCount,
                    SecondaryValue = w.ActuallyPaid
                }).ToList(),
                
                DailySalesChart = dailySales.Select(d => new ChartDataPoint
                {
                    Label = d.Date.ToString("MM/dd"),
                    Value = d.Value,
                    Count = d.Count,
                    SecondaryValue = d.ActuallyPaid
                }).ToList(),
                
                // Top performers
                TopRetailers = salesByRetailer.Select(r => new TopRetailerItem
                {
                    RetailerId = r.RetailerId,
                    RetailerName = r.RetailerName,
                    OrderCount = r.OrderCount,
                    TotalRevenue = r.TotalAmount,
                    ActuallyPaid = r.ActuallyPaid
                }).ToList(),
                
                TopWholesalers = salesByWholesaler.Select(w => new TopWholesalerItem
                {
                    WholesalerId = w.WholesalerId,
                    WholesalerName = w.WholesalerName,
                    OrderCount = w.OrderCount,
                    TotalSpent = w.TotalAmount,
                    ActuallyPaid = w.ActuallyPaid
                }).ToList(),
                
                TopProducts = topProducts
            };

            return View(viewModel);
        }

        // GET: Reports/Payments - Enhanced payment reports for Admin
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Payments(DateTime? startDate, DateTime? endDate, string retailerId = null, string wholesalerId = null, string paymentMethod = null)
        {
            // Default to current month if no dates provided
            startDate ??= new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            endDate ??= DateTime.UtcNow.AddDays(1).AddSeconds(-1); // Include all of the end date

            // Get all retailers and wholesalers for filter dropdowns
            var retailers = await _userManager.GetUsersInRoleAsync("Retailer");
            var wholesalers = await _userManager.GetUsersInRoleAsync("Wholesaler");

            // Build query with includes (exclude payments for cancelled orders)
            // IMPORTANT: Use Order.OrderDate for consistency with Sales and Financial reports
            var query = _context.Payments
                .Include(p => p.Transaction)
                    .ThenInclude(t => t.Order)
                        .ThenInclude(o => o.Retailer)
                .Include(p => p.Transaction)
                    .ThenInclude(t => t.Order)
                        .ThenInclude(o => o.Wholesaler)
                .Include(p => p.Transaction)
                    .ThenInclude(t => t.Order)
                        .ThenInclude(o => o.Items)
                .Where(p => p.Transaction.Order.OrderDate >= startDate && p.Transaction.Order.OrderDate <= endDate &&
                          p.Transaction.Order.Status != OrderStatus.Cancelled); // Exclude payments for cancelled orders

            // Apply filters if provided
            if (!string.IsNullOrEmpty(retailerId))
            {
                query = query.Where(p => p.Transaction.RetailerId == retailerId);
            }

            if (!string.IsNullOrEmpty(wholesalerId))
            {
                query = query.Where(p => p.Transaction.WholesalerId == wholesalerId);
            }

            if (!string.IsNullOrEmpty(paymentMethod))
            {
                if (Enum.TryParse<PaymentMethod>(paymentMethod, out var method))
                {
                    query = query.Where(p => p.Method == method);
                }
            }

            // Execute query
            var payments = await query
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            // Group payments by method for chart data
            var paymentsByMethod = payments
                .GroupBy(p => p.Method)
                .Select(g => new
                {
                    Method = g.Key.ToString(),
                    Count = g.Count(),
                    Value = g.Sum(p => p.Amount)
                })
                .ToList();

            // Group payments by date for trend chart
            var paymentsByDate = payments
                .GroupBy(p => p.PaymentDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count(),
                    Value = g.Sum(p => p.Amount)
                })
                .OrderBy(d => d.Date)
                .ToList();

            // Group payments by retailer
            var paymentsByRetailer = payments
                .GroupBy(p => p.Transaction?.RetailerId)
                .Where(g => g.Key != null)
                .Select(g => new
                {
                    RetailerId = g.Key,
                    RetailerName = g.First().Transaction?.Order?.Retailer?.BusinessName ?? "Unknown",
                    Count = g.Count(),
                    Value = g.Sum(p => p.Amount)
                })
                .OrderByDescending(r => r.Value)
                .Take(10)
                .ToList();

            // Group payments by wholesaler
            var paymentsByWholesaler = payments
                .GroupBy(p => p.Transaction?.WholesalerId)
                .Where(g => g.Key != null)
                .Select(g => new
                {
                    WholesalerId = g.Key,
                    WholesalerName = g.First().Transaction?.Order?.Wholesaler?.BusinessName ?? "Unknown",
                    Count = g.Count(),
                    Value = g.Sum(p => p.Amount)
                })
                .OrderByDescending(w => w.Value)
                .Take(10)
                .ToList();

            // Calculate totals
            var totalPayments = payments.Count;
            var totalAmount = payments.Sum(p => p.Amount);

            // Calculate collection rate correctly - get all transactions in the date range (exclude cancelled orders)
            var allTransactionsInPeriod = await _context.Transactions
                .Include(t => t.Order)
                .Include(t => t.Payments)
                .Where(t => t.Order.OrderDate >= startDate && t.Order.OrderDate <= endDate &&
                          t.Order.Status != OrderStatus.Cancelled && // Exclude cancelled orders
                          (retailerId == null || t.RetailerId == retailerId) &&
                          (wholesalerId == null || t.WholesalerId == wholesalerId))
                .ToListAsync();

            // Calculate total amount owed and total amount paid
            var totalAmountOwed = allTransactionsInPeriod.Sum(t => t.Amount);
            var totalAmountPaid = allTransactionsInPeriod.Sum(t => t.Payments?.Sum(p => p.Amount) ?? 0);
            var totalPendingAmount = totalAmountOwed - totalAmountPaid;
            var collectionRate = totalAmountOwed > 0 ? (totalAmountPaid / totalAmountOwed) * 100 : 0;

            // Build the enhanced view model
            var viewModel = new PaymentsReportViewModel
            {
                // Dates and filters
                StartDate = startDate.Value,
                EndDate = endDate.Value,
                RetailerId = retailerId,
                WholesalerId = wholesalerId,
                PaymentMethodFilter = paymentMethod,
                
                // Collections for dropdowns
                Payments = payments,
                Retailers = retailers,
                Wholesalers = wholesalers,
                PaymentMethods = Enum.GetValues(typeof(PaymentMethod)).Cast<PaymentMethod>().ToList(),
                
                // Summary metrics
                TotalPayments = totalPayments,
                TotalAmount = totalAmount,
                TotalPendingAmount = totalPendingAmount,
                CollectionRate = collectionRate,
                
                // Chart data
                PaymentsByMethodChart = paymentsByMethod.Select(p => new ChartDataPoint
                {
                    Label = p.Method,
                    Value = p.Value,
                    Count = p.Count
                }).ToList(),
                
                PaymentsTrendChart = paymentsByDate.Select(d => new ChartDataPoint
                {
                    Label = d.Date.ToString("MM/dd"),
                    Value = d.Value,
                    Count = d.Count
                }).ToList(),
                
                PaymentsByRetailerChart = paymentsByRetailer.Select(r => new ChartDataPoint
                {
                    Label = r.RetailerName,
                    Value = r.Value,
                    Count = r.Count
                }).ToList(),
                
                PaymentsByWholesalerChart = paymentsByWholesaler.Select(w => new ChartDataPoint
                {
                    Label = w.WholesalerName,
                    Value = w.Value,
                    Count = w.Count
                }).ToList(),
                
                // Top performers
                TopRetailers = paymentsByRetailer.Select(r => new TopRetailerItem
                {
                    RetailerId = r.RetailerId,
                    RetailerName = r.RetailerName,
                    OrderCount = r.Count,
                    TotalRevenue = r.Value
                }).ToList(),
                
                TopWholesalers = paymentsByWholesaler.Select(w => new TopWholesalerItem
                {
                    WholesalerId = w.WholesalerId,
                    WholesalerName = w.WholesalerName,
                    OrderCount = w.Count,
                    TotalSpent = w.Value
                }).ToList()
            };

            return View(viewModel);
        }

        // GET: Reports/Users - User activity report for Admin
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Users()
        {
            // Get all users with their roles
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = new List<UserReportViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var userRole = roles.FirstOrDefault() ?? "No Role";

                var orderCount = 0;
                decimal totalSpent = 0;
                decimal totalEarned = 0;

                if (userRole == "Retailer")
                {
                    // For retailers, count orders and total spent
                    orderCount = await _context.Orders
                        .CountAsync(o => o.RetailerId == user.Id);
                    
                    totalSpent = await _context.Transactions
                        .Where(t => t.RetailerId == user.Id)
                        .SumAsync(t => t.AmountPaid);
                }
                else if (userRole == "Wholesaler")
                {
                    // For wholesalers, count orders received and total earned
                    orderCount = await _context.Orders
                        .CountAsync(o => o.WholesalerId == user.Id);
                    
                    totalEarned = await _context.Transactions
                        .Where(t => t.WholesalerId == user.Id)
                        .SumAsync(t => t.AmountPaid);
                }

                userViewModels.Add(new UserReportViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    BusinessName = user.BusinessName,
                    Role = userRole,
                    RegistrationDate = DateTime.UtcNow,
                    OrderCount = orderCount,
                    TotalSpent = totalSpent,
                    TotalEarned = totalEarned
                });
            }

            return View(userViewModels);
        }

        // GET: Reports/RetailerPerformance - Retailer's own performance report
        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> RetailerPerformance(DateTime? startDate, DateTime? endDate)
        {
            // Default to current month if no dates provided
            startDate ??= new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            endDate ??= DateTime.UtcNow;

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Get retailer's orders within date range (exclude cancelled orders from payment calculations)
            var orders = await _context.Orders
                .Include(o => o.Wholesaler)
                .Include(o => o.Items)
                .Include(o => o.Transaction)
                    .ThenInclude(t => t.Payments)
                .Where(o => o.RetailerId == user.Id && 
                        o.OrderDate >= startDate && 
                        o.OrderDate <= endDate &&
                        o.Status != OrderStatus.Cancelled) // Exclude cancelled orders
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Reload transactions to ensure we have the latest data
            var orderIds = orders.Select(o => o.Id).ToList();
            var transactions = await _context.Transactions
                .Include(t => t.Payments)
                .Where(t => orderIds.Contains(t.OrderId))
                .ToListAsync();
                
            // Create a lookup dictionary for quick access
            var transactionLookup = transactions.GroupBy(t => t.OrderId).ToDictionary(g => g.Key, g => g.First());

            // Fix transaction statuses in the database
            bool hasChanges = false;
            foreach (var order in orders)
            {
                if (order.Transaction != null)
                {
                    // Calculate the actual amount paid from the Payments collection
                    decimal actualAmountPaid = order.Transaction.Payments?.Sum(p => p.Amount) ?? 0;
                    
                    // Update the AmountPaid field if it's incorrect
                    if (order.Transaction.AmountPaid != actualAmountPaid)
                    {
                        order.Transaction.AmountPaid = actualAmountPaid;
                        hasChanges = true;
                    }
                    
                    var totalAmount = order.Transaction.Amount;
                    
                    // Update transaction status based on actual payment
                    if (actualAmountPaid >= totalAmount && order.Transaction.Status != TransactionStatus.Completed)
                    {
                        order.Transaction.Status = TransactionStatus.Completed;
                        _context.Update(order.Transaction);
                        hasChanges = true;
                    }
                    else if (actualAmountPaid > 0 && actualAmountPaid < totalAmount && 
                             order.Transaction.Status == TransactionStatus.Pending)
                    {
                        order.Transaction.Status = TransactionStatus.PartiallyPaid;
                        _context.Update(order.Transaction);
                        hasChanges = true;
                    }
                }
            }
            
            // Save any status changes to the database
            if (hasChanges)
            {
                await _context.SaveChangesAsync();
                
                // Reload transactions after saving changes
                transactions = await _context.Transactions
                    .Include(t => t.Payments)
                    .Where(t => orderIds.Contains(t.OrderId))
                    .ToListAsync();
                    
                // Update the lookup dictionary
                transactionLookup = transactions.GroupBy(t => t.OrderId).ToDictionary(g => g.Key, g => g.First());
            }

            // Enhanced Order Metrics
            var totalOrders = orders.Count;
            var completedOrders = orders.Count(o => o.Status == OrderStatus.Completed);
            var pendingOrders = orders.Count(o => o.Status == OrderStatus.Pending);
            var processingOrders = orders.Count(o => o.Status == OrderStatus.Processing);
            var shippedOrders = orders.Count(o => o.Status == OrderStatus.Shipped);
            var deliveredOrders = orders.Count(o => o.Status == OrderStatus.Delivered);
            var cancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled);

            // Enhanced Financial Metrics
            var totalOrderValue = orders.Sum(o => o.TotalAmount);
            var actualAmountSpent = orders
                .Where(o => transactionLookup.ContainsKey(o.Id))
                .Sum(o => transactionLookup[o.Id].AmountPaid);
            var outstandingBalance = orders
                .Where(o => transactionLookup.ContainsKey(o.Id))
                .Sum(o => {
                    var transaction = transactionLookup[o.Id];
                    return transaction.Amount - transaction.AmountPaid;
                });
            var avgOrderValue = totalOrders > 0 ? totalOrderValue / totalOrders : 0;

            // Performance Metrics
            var orderItems = orders.SelectMany(o => o.Items ?? new List<OrderItem>()).ToList();
            var totalProductsPurchased = orderItems.Select(oi => oi.WholesalerProductId).Distinct().Count();
            var totalQuantityOrdered = orderItems.Sum(oi => oi.Quantity);
            var avgProductPrice = orderItems.Any() ? orderItems.Average(oi => oi.UnitPrice) : 0;
            var uniqueWholesalers = orders.Select(o => o.WholesalerId).Distinct().Count();
            var orderFrequency = totalOrders > 0 ? totalOrders / Math.Max(1, (endDate.Value - startDate.Value).TotalDays / 30) : 0;

            // Business Insights
            var preferredWholesaler = orders
                .GroupBy(o => o.WholesalerId)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.First().Wholesaler?.BusinessName ?? "None";

            var categorySpending = await GetCategorySpendingAnalysis(user.Id, startDate.Value, endDate.Value);
            var mostOrderedCategory = categorySpending.OrderByDescending(c => c.TotalSpent).FirstOrDefault()?.CategoryName ?? "None";

            // Monthly trends for growth calculation
            var monthlyTrends = await GetMonthlySpendingTrends(user.Id, startDate.Value, endDate.Value);
            var monthlySpendTrend = CalculateGrowthRate(monthlyTrends);

            // Payment reliability assessment
            var paymentReliability = actualAmountSpent / Math.Max(1, totalOrderValue) >= 0.9m ? "Excellent" :
                                   actualAmountSpent / Math.Max(1, totalOrderValue) >= 0.7m ? "Good" : "Poor";

            // Top wholesalers by order count and amount
            var topWholesalers = orders
                .GroupBy(o => o.WholesalerId)
                .Select(g => new TopWholesalerItem
                {
                    WholesalerId = g.Key,
                    WholesalerName = g.First().Wholesaler?.BusinessName ?? "Unknown",
                    OrderCount = g.Count(),
                    TotalSpent = g.Sum(o => o.TotalAmount),
                    ActuallyPaid = g.Where(o => transactionLookup.ContainsKey(o.Id))
                                    .Sum(o => transactionLookup[o.Id].AmountPaid)
                })
                .OrderByDescending(w => w.TotalSpent)
                .Take(5)
                .ToList();

            // Chart Data
            var paymentStatusChart = orders
                .Where(o => transactionLookup.ContainsKey(o.Id))
                .GroupBy(o => transactionLookup[o.Id].Status)
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key.ToString(),
                    Count = g.Count(),
                    Value = g.Sum(o => transactionLookup[o.Id].AmountPaid)
                })
                .ToList();

            var orderStatusChart = new List<ChartDataPoint>
            {
                new ChartDataPoint { Label = "Completed", Count = completedOrders, Value = completedOrders },
                new ChartDataPoint { Label = "Pending", Count = pendingOrders, Value = pendingOrders },
                new ChartDataPoint { Label = "Processing", Count = processingOrders, Value = processingOrders },
                new ChartDataPoint { Label = "Shipped", Count = shippedOrders, Value = shippedOrders },
                new ChartDataPoint { Label = "Delivered", Count = deliveredOrders, Value = deliveredOrders },
                new ChartDataPoint { Label = "Cancelled", Count = cancelledOrders, Value = cancelledOrders }
            }.Where(c => c.Count > 0).ToList();

            var monthlySpendingChart = monthlyTrends.Select(t => new ChartDataPoint
            {
                Label = t.Month,
                Value = t.AmountSpent,
                Count = t.OrderCount
            }).ToList();

            var categoryDistributionChart = categorySpending.Select(c => new ChartDataPoint
            {
                Label = c.CategoryName,
                Value = c.TotalSpent,
                Count = c.OrderCount
            }).ToList();

            // Recent orders with payment status
            var recentOrders = orders.Take(10)
                .Select(o => {
                    // Use the transaction from our lookup dictionary to ensure we have the latest data
                    Transaction transaction = null;
                    if (transactionLookup.TryGetValue(o.Id, out var lookupTransaction))
                    {
                        transaction = lookupTransaction;
                    }
                    else
                    {
                        transaction = o.Transaction;
                    }
                    
                    // Calculate payment values
                    var amountPaid = transaction?.AmountPaid ?? 0;
                    var totalAmount = transaction?.Amount ?? o.TotalAmount;
                    var remainingAmount = totalAmount - amountPaid;
                    
                    // Use the transaction status from our lookup
                    TransactionStatus paymentStatus = transaction?.Status ?? TransactionStatus.Pending;
                    
                    return new OrderWithPaymentStatus
                    {
                        OrderId = o.Id,
                        OrderDate = o.OrderDate,
                        TotalAmount = totalAmount,
                        AmountPaid = amountPaid,
                        RemainingAmount = remainingAmount,
                        OrderStatus = o.Status,
                        PaymentStatus = paymentStatus
                    };
                })
                .ToList();

            var viewModel = new RetailerReportViewModel
            {
                StartDate = startDate.Value,
                EndDate = endDate.Value,
                RetailerId = user.Id,
                RetailerName = user.BusinessName ?? user.UserName,
                
                // Order Metrics
                TotalOrders = totalOrders,
                CompletedOrders = completedOrders,
                PendingOrders = pendingOrders,
                ProcessingOrders = processingOrders,
                ShippedOrders = shippedOrders,
                DeliveredOrders = deliveredOrders,
                CancelledOrders = cancelledOrders,
                
                // Financial Metrics
                TotalOrderValue = totalOrderValue,
                ActualAmountSpent = actualAmountSpent,
                OutstandingBalance = outstandingBalance,
                AvgOrderValue = avgOrderValue,
                
                // Performance Metrics
                TotalProductsPurchased = totalProductsPurchased,
                TotalQuantityOrdered = totalQuantityOrdered,
                AvgProductPrice = (decimal)avgProductPrice,
                UniqueWholesalers = uniqueWholesalers,
                OrderFrequency = (decimal)orderFrequency,
                
                // Business Insights
                PreferredWholesaler = preferredWholesaler,
                MostOrderedCategory = mostOrderedCategory,
                MonthlySpendTrend = monthlySpendTrend,
                PaymentReliability = paymentReliability,
                
                // Collections
                RecentOrders = recentOrders,
                TopWholesalers = topWholesalers,
                CategorySpending = categorySpending,
                MonthlyTrends = monthlyTrends,
                
                // Chart Data
                PaymentStatusChart = paymentStatusChart,
                OrderStatusChart = orderStatusChart,
                MonthlySpendingChart = monthlySpendingChart,
                CategoryDistributionChart = categoryDistributionChart
            };

            return View(viewModel);
        }

        // GET: Reports/WholesalerPerformance - Wholesaler's own performance report
        [Authorize(Roles = "Wholesaler")]
        public async Task<IActionResult> WholesalerPerformance(DateTime? startDate, DateTime? endDate)
        {
            // Default to current month if no dates provided
            startDate ??= new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            endDate ??= DateTime.UtcNow;

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Get wholesaler's orders within date range (exclude cancelled orders from payment calculations)
            var orders = await _context.Orders
                .Include(o => o.Retailer)
                .Include(o => o.Items)
                .Include(o => o.Transaction)
                    .ThenInclude(t => t.Payments)
                .Where(o => o.WholesalerId == user.Id && 
                        o.OrderDate >= startDate && 
                        o.OrderDate <= endDate &&
                        o.Status != OrderStatus.Cancelled) // Exclude cancelled orders
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Reload transactions to ensure we have the latest data
            var orderIds = orders.Select(o => o.Id).ToList();
            var transactions = await _context.Transactions
                .Include(t => t.Payments)
                .Where(t => orderIds.Contains(t.OrderId))
                .ToListAsync();
                
            // Create a lookup dictionary for quick access
            var transactionLookup = transactions.GroupBy(t => t.OrderId).ToDictionary(g => g.Key, g => g.First());

            // Fix transaction statuses in the database
            bool hasChanges = false;
            foreach (var order in orders)
            {
                if (order.Transaction != null)
                {
                    // Calculate the actual amount paid from the Payments collection
                    decimal actualAmountPaid = order.Transaction.Payments?.Sum(p => p.Amount) ?? 0;
                    
                    // Update the AmountPaid field if it's incorrect
                    if (order.Transaction.AmountPaid != actualAmountPaid)
                    {
                        order.Transaction.AmountPaid = actualAmountPaid;
                        hasChanges = true;
                    }
                    
                    var totalAmount = order.Transaction.Amount;
                    
                    // Update transaction status based on actual payment
                    if (actualAmountPaid >= totalAmount && order.Transaction.Status != TransactionStatus.Completed)
                    {
                        order.Transaction.Status = TransactionStatus.Completed;
                        _context.Update(order.Transaction);
                        hasChanges = true;
                    }
                    else if (actualAmountPaid > 0 && actualAmountPaid < totalAmount && 
                             order.Transaction.Status == TransactionStatus.Pending)
                    {
                        order.Transaction.Status = TransactionStatus.PartiallyPaid;
                        _context.Update(order.Transaction);
                        hasChanges = true;
                    }
                }
            }
            
            // Save any status changes to the database
            if (hasChanges)
            {
                await _context.SaveChangesAsync();
                
                // Reload transactions after saving changes
                transactions = await _context.Transactions
                    .Include(t => t.Payments)
                    .Where(t => orderIds.Contains(t.OrderId))
                    .ToListAsync();
                    
                // Update the lookup dictionary
                transactionLookup = transactions.GroupBy(t => t.OrderId).ToDictionary(g => g.Key, g => g.First());
            }

            // Enhanced Order Metrics
            var totalOrders = orders.Count;
            var completedOrders = orders.Count(o => o.Status == OrderStatus.Completed);
            var pendingOrders = orders.Count(o => o.Status == OrderStatus.Pending);
            var processingOrders = orders.Count(o => o.Status == OrderStatus.Processing);
            var shippedOrders = orders.Count(o => o.Status == OrderStatus.Shipped);
            var deliveredOrders = orders.Count(o => o.Status == OrderStatus.Delivered);
            var cancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled);

            // Enhanced Financial Metrics
            var totalOrderValue = orders.Sum(o => o.TotalAmount);
            var actualRevenue = orders
                .Where(o => transactionLookup.ContainsKey(o.Id))
                .Sum(o => transactionLookup[o.Id].AmountPaid);
            var outstandingPayments = orders
                .Where(o => transactionLookup.ContainsKey(o.Id))
                .Sum(o => {
                    var transaction = transactionLookup[o.Id];
                    return transaction.Amount - transaction.AmountPaid;
                });
            var avgOrderValue = totalOrders > 0 ? totalOrderValue / totalOrders : 0;

            // Performance Metrics
            var orderItems = orders.SelectMany(o => o.Items ?? new List<OrderItem>()).ToList();
            var totalProductsSold = orderItems.Select(oi => oi.WholesalerProductId).Distinct().Count();
            var totalQuantitySold = orderItems.Sum(oi => oi.Quantity);
            var avgSellPrice = orderItems.Any() ? orderItems.Average(oi => oi.UnitPrice) : 0;
            var uniqueRetailers = orders.Select(o => o.RetailerId).Distinct().Count();
            var orderFrequency = totalOrders > 0 ? totalOrders / Math.Max(1, (endDate.Value - startDate.Value).TotalDays / 30) : 0;

            // Get inventory data for analysis
            var productQueryResult = await _context.WholesalerProducts
                .Include(wp => wp.Product)
                .Where(wp => wp.WholesalerId == user.Id)
                .ToListAsync();

            // Get inventory metrics
            var activeProducts = productQueryResult.Count(wp => wp.IsActive);
            var lowStockProducts = productQueryResult.Count(wp => wp.StockQuantity <= 50);

            // Business Insights
            var topRetailer = orders
                .GroupBy(o => o.RetailerId)
                .OrderByDescending(g => g.Sum(o => o.TotalAmount))
                .FirstOrDefault()?.First().Retailer?.BusinessName ?? "None";

            var categoryPerformance = await GetCategoryPerformanceAnalysis(user.Id, startDate.Value, endDate.Value);
            var bestSellingCategory = categoryPerformance.OrderByDescending(c => c.TotalRevenue).FirstOrDefault()?.CategoryName ?? "None";

            // Monthly trends for growth calculation
            var monthlyTrends = await GetMonthlyRevenueTrends(user.Id, startDate.Value, endDate.Value);
            var monthlyRevenueTrend = CalculateRevenueGrowthRate(monthlyTrends);

            // Inventory health assessment
            var inventoryTurnover = totalQuantitySold > 0 && productQueryResult.Any() ? 
                totalQuantitySold / (decimal)productQueryResult.Sum(wp => wp.StockQuantity) : 0;
            var inventoryHealth = inventoryTurnover >= 0.5m ? "Excellent" :
                                inventoryTurnover >= 0.3m ? "Good" : "Poor";

            var inventoryStatus = await GetInventoryStatusAnalysis(user.Id, startDate.Value, endDate.Value);

            // Top retailers by order count and amount
            var topRetailers = orders
                .GroupBy(o => o.RetailerId)
                .Select(g => new TopRetailerItem
                {
                    RetailerId = g.Key,
                    RetailerName = g.First().Retailer?.BusinessName ?? "Unknown",
                    OrderCount = g.Count(),
                    TotalRevenue = g.Sum(o => o.TotalAmount),
                    ActuallyPaid = g.Where(o => transactionLookup.ContainsKey(o.Id))
                                    .Sum(o => transactionLookup[o.Id].AmountPaid)
                })
                .OrderByDescending(r => r.TotalRevenue)
                .Take(5)
                .ToList();

            // Chart Data
            var revenueChart = orders
                .Where(o => transactionLookup.ContainsKey(o.Id))
                .GroupBy(o => new { Month = o.OrderDate.Month, Year = o.OrderDate.Year })
                .Select(g => new ChartDataPoint
                {
                    Label = $"{g.Key.Month}/{g.Key.Year}",
                    Count = g.Count(),
                    Value = g.Sum(o => {
                        var transaction = transactionLookup[o.Id];
                        return transaction.AmountPaid;
                    })
                })
                .OrderBy(p => p.Label)
                .ToList();

            var orderStatusChart = new List<ChartDataPoint>
            {
                new ChartDataPoint { Label = "Completed", Count = completedOrders, Value = completedOrders },
                new ChartDataPoint { Label = "Pending", Count = pendingOrders, Value = pendingOrders },
                new ChartDataPoint { Label = "Processing", Count = processingOrders, Value = processingOrders },
                new ChartDataPoint { Label = "Shipped", Count = shippedOrders, Value = shippedOrders },
                new ChartDataPoint { Label = "Delivered", Count = deliveredOrders, Value = deliveredOrders },
                new ChartDataPoint { Label = "Cancelled", Count = cancelledOrders, Value = cancelledOrders }
            }.Where(c => c.Count > 0).ToList();

            var paymentStatusChart = orders
                .Where(o => transactionLookup.ContainsKey(o.Id))
                .GroupBy(o => transactionLookup[o.Id].Status)
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key.ToString(),
                    Count = g.Count(),
                    Value = g.Sum(o => transactionLookup[o.Id].AmountPaid)
                })
                .ToList();

            var categoryRevenueChart = categoryPerformance.Select(c => new ChartDataPoint
            {
                Label = c.CategoryName,
                Value = c.TotalRevenue,
                Count = c.OrderCount
            }).ToList();

            var inventoryDistributionChart = new List<ChartDataPoint>
            {
                new ChartDataPoint { Label = "Critical Stock", Count = productQueryResult.Count(wp => wp.StockQuantity <= 10), Value = productQueryResult.Count(wp => wp.StockQuantity <= 10) },
                new ChartDataPoint { Label = "Low Stock", Count = productQueryResult.Count(wp => wp.StockQuantity > 10 && wp.StockQuantity <= 50), Value = productQueryResult.Count(wp => wp.StockQuantity > 10 && wp.StockQuantity <= 50) },
                new ChartDataPoint { Label = "Normal Stock", Count = productQueryResult.Count(wp => wp.StockQuantity > 50 && wp.StockQuantity <= 200), Value = productQueryResult.Count(wp => wp.StockQuantity > 50 && wp.StockQuantity <= 200) },
                new ChartDataPoint { Label = "High Stock", Count = productQueryResult.Count(wp => wp.StockQuantity > 200), Value = productQueryResult.Count(wp => wp.StockQuantity > 200) }
            }.Where(c => c.Count > 0).ToList();

            // Recent orders with payment status
            var recentOrders = orders
                .Take(10)
                .Select(o => {
                    // Use the transaction from our lookup dictionary to ensure we have the latest data
                    Transaction transaction = null;
                    if (transactionLookup.TryGetValue(o.Id, out var lookupTransaction))
                    {
                        transaction = lookupTransaction;
                    }
                    else
                    {
                        transaction = o.Transaction;
                    }
                    
                    // Calculate payment values
                    var amountPaid = transaction?.AmountPaid ?? 0;
                    var totalAmount = transaction?.Amount ?? o.TotalAmount;
                    var remainingAmount = totalAmount - amountPaid;
                    
                    // Use the transaction status from our lookup
                    TransactionStatus paymentStatus = transaction?.Status ?? TransactionStatus.Pending;
                    
                    return new OrderWithPaymentStatus
                    {
                        OrderId = o.Id,
                        OrderDate = o.OrderDate,
                        TotalAmount = totalAmount,
                        AmountPaid = amountPaid,
                        RemainingAmount = remainingAmount,
                        OrderStatus = o.Status,
                        PaymentStatus = paymentStatus
                    };
                })
                .ToList();

            // Top products by quantity sold
                
            // Then perform the join on the client side
            var topProducts = productQueryResult
                .Join(orderItems,
                    wp => wp.Id,
                    oi => oi.WholesalerProductId,
                    (wp, oi) => new {
                        ProductId = wp.ProductId,
                        ProductName = wp.Product.Name,
                        Quantity = oi.Quantity,
                        Revenue = oi.Quantity * oi.UnitPrice
                    })
                .GroupBy(x => new { x.ProductId, x.ProductName })
                .Select(g => new TopProductItem
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    QuantitySold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.Revenue)
                })
                .OrderByDescending(p => p.TotalRevenue)
                .Take(5)
                .ToList();

            var viewModel = new WholesalerReportViewModel
            {
                StartDate = startDate.Value,
                EndDate = endDate.Value,
                WholesalerId = user.Id,
                WholesalerName = user.BusinessName ?? user.UserName,
                
                // Order Metrics
                TotalOrders = totalOrders,
                CompletedOrders = completedOrders,
                PendingOrders = pendingOrders,
                ProcessingOrders = processingOrders,
                ShippedOrders = shippedOrders,
                DeliveredOrders = deliveredOrders,
                CancelledOrders = cancelledOrders,
                
                // Financial Metrics
                TotalOrderValue = totalOrderValue,
                ActualRevenue = actualRevenue,
                OutstandingPayments = outstandingPayments,
                AvgOrderValue = avgOrderValue,
                
                // Performance Metrics
                TotalProductsSold = totalProductsSold,
                TotalQuantitySold = totalQuantitySold,
                AvgSellPrice = (decimal)avgSellPrice,
                UniqueRetailers = uniqueRetailers,
                OrderFrequency = (decimal)orderFrequency,
                ActiveProducts = activeProducts,
                LowStockProducts = lowStockProducts,
                
                // Business Insights
                TopRetailer = topRetailer,
                BestSellingCategory = bestSellingCategory,
                MonthlyRevenueTrend = monthlyRevenueTrend,
                InventoryHealth = inventoryHealth,
                InventoryTurnover = inventoryTurnover,
                
                // Collections
                RecentOrders = recentOrders,
                TopRetailers = topRetailers,
                TopProducts = topProducts,
                CategoryPerformance = categoryPerformance,
                MonthlyTrends = monthlyTrends,
                InventoryStatus = inventoryStatus,
                
                // Chart Data
                RevenueChart = revenueChart,
                OrderStatusChart = orderStatusChart,
                PaymentStatusChart = paymentStatusChart,
                CategoryRevenueChart = categoryRevenueChart,
                InventoryDistributionChart = inventoryDistributionChart
            };

            return View(viewModel);
        }

        // GET: Reports/RetailerInventoryAnalysis - Detailed inventory and ordering patterns for Retailers
        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> RetailerInventoryAnalysis(DateTime? startDate, DateTime? endDate)
        {
            // Default to last 3 months if no dates provided
            startDate ??= DateTime.UtcNow.AddMonths(-3);
            endDate ??= DateTime.UtcNow;

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Get detailed order items data
            var orderItems = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.WholesalerProduct)
                    .ThenInclude(wp => wp.Product)
                        .ThenInclude(p => p.Category)
                .Include(oi => oi.WholesalerProduct)
                    .ThenInclude(wp => wp.Wholesaler)
                .Where(oi => oi.Order.RetailerId == user.Id && 
                        oi.Order.OrderDate >= startDate && 
                        oi.Order.OrderDate <= endDate)
                .ToListAsync();

            // Product performance analysis
            var productAnalysis = orderItems
                .GroupBy(oi => new { 
                    ProductId = oi.WholesalerProduct.ProductId,
                    ProductName = oi.WholesalerProduct.Product.Name,
                    CategoryName = oi.WholesalerProduct.Product.Category.Name
                })
                .Select(g => new {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    CategoryName = g.Key.CategoryName,
                    TotalQuantity = g.Sum(oi => oi.Quantity),
                    TotalSpent = g.Sum(oi => oi.UnitPrice * oi.Quantity),
                    OrderCount = g.Count(),
                    AvgOrderQuantity = g.Average(oi => oi.Quantity),
                    AvgPrice = g.Average(oi => oi.UnitPrice),
                    FirstOrderDate = g.Min(oi => oi.Order.OrderDate),
                    LastOrderDate = g.Max(oi => oi.Order.OrderDate),
                    DaysBetweenOrders = g.Count() > 1 ? (g.Max(oi => oi.Order.OrderDate) - g.Min(oi => oi.Order.OrderDate)).TotalDays / (g.Count() - 1) : 0
                })
                .OrderByDescending(p => p.TotalSpent)
                .ToList();

            // Category analysis
            var categoryAnalysis = orderItems
                .GroupBy(oi => oi.WholesalerProduct.Product.Category.Name)
                .Select(g => new {
                    CategoryName = g.Key,
                    TotalQuantity = g.Sum(oi => oi.Quantity),
                    TotalSpent = g.Sum(oi => oi.UnitPrice * oi.Quantity),
                    ProductCount = g.Select(oi => oi.WholesalerProduct.ProductId).Distinct().Count(),
                    AvgOrderValue = g.Average(oi => oi.UnitPrice * oi.Quantity)
                })
                .OrderByDescending(c => c.TotalSpent)
                .ToList();

            // Seasonal trends (monthly analysis)
            var monthlyTrends = orderItems
                .GroupBy(oi => new { Year = oi.Order.OrderDate.Year, Month = oi.Order.OrderDate.Month })
                .Select(g => new {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    TotalQuantity = g.Sum(oi => oi.Quantity),
                    TotalSpent = g.Sum(oi => oi.UnitPrice * oi.Quantity),
                    UniqueProducts = g.Select(oi => oi.WholesalerProduct.ProductId).Distinct().Count()
                })
                .OrderBy(m => m.Month)
                .ToList();

            // Supplier performance
            var supplierAnalysis = orderItems
                .GroupBy(oi => new {
                    SupplierId = oi.WholesalerProduct.WholesalerId,
                    SupplierName = oi.WholesalerProduct.Wholesaler.BusinessName ?? oi.WholesalerProduct.Wholesaler.UserName
                })
                .Select(g => new {
                    SupplierId = g.Key.SupplierId,
                    SupplierName = g.Key.SupplierName,
                    TotalSpent = g.Sum(oi => oi.UnitPrice * oi.Quantity),
                    OrderCount = g.Select(oi => oi.OrderId).Distinct().Count(),
                    ProductVariety = g.Select(oi => oi.WholesalerProduct.ProductId).Distinct().Count(),
                    AvgDeliveryValue = g.Sum(oi => oi.UnitPrice * oi.Quantity) / g.Select(oi => oi.OrderId).Distinct().Count(),
                    ReorderFrequency = g.Count() / Math.Max(1, (DateTime.UtcNow - g.Min(oi => oi.Order.OrderDate)).TotalDays / 30) // Orders per month
                })
                .OrderByDescending(s => s.TotalSpent)
                .ToList();

            var viewModel = new RetailerInventoryAnalysisViewModel
            {
                StartDate = startDate.Value,
                EndDate = endDate.Value,
                ProductAnalysis = productAnalysis.Select(p => new ProductAnalysisItem
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    CategoryName = p.CategoryName,
                    TotalQuantity = p.TotalQuantity,
                    TotalSpent = p.TotalSpent,
                    OrderCount = p.OrderCount,
                    AvgOrderQuantity = (decimal)p.AvgOrderQuantity,
                    AvgPrice = p.AvgPrice,
                    ReorderFrequency = p.DaysBetweenOrders > 0 ? (decimal)p.DaysBetweenOrders : 0,
                    LastOrderDate = p.LastOrderDate
                }).Take(20).ToList(),
                
                CategoryAnalysis = categoryAnalysis.Select(c => new CategoryAnalysisItem
                {
                    CategoryName = c.CategoryName,
                    TotalQuantity = c.TotalQuantity,
                    TotalSpent = c.TotalSpent,
                    ProductCount = c.ProductCount,
                    AvgOrderValue = (decimal)c.AvgOrderValue
                }).ToList(),
                
                MonthlyTrends = monthlyTrends.Select(m => new MonthlyTrendItem
                {
                    Month = m.Month,
                    TotalQuantity = m.TotalQuantity,
                    TotalSpent = m.TotalSpent,
                    UniqueProducts = m.UniqueProducts
                }).ToList(),
                
                SupplierAnalysis = supplierAnalysis.Select(s => new SupplierAnalysisItem
                {
                    SupplierId = s.SupplierId,
                    SupplierName = s.SupplierName,
                    TotalSpent = s.TotalSpent,
                    OrderCount = s.OrderCount,
                    ProductVariety = s.ProductVariety,
                    AvgDeliveryValue = s.AvgDeliveryValue,
                    ReorderFrequency = (decimal)s.ReorderFrequency
                }).Take(10).ToList()
            };

            return View(viewModel);
        }

        // GET: Reports/WholesalerBusinessInsights - Comprehensive business analytics for Wholesalers
        [Authorize(Roles = "Wholesaler")]
        public async Task<IActionResult> WholesalerBusinessInsights(DateTime? startDate, DateTime? endDate)
        {
            // Default to last 6 months if no dates provided
            startDate ??= DateTime.UtcNow.AddMonths(-6);
            endDate ??= DateTime.UtcNow;

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Get comprehensive order and product data
            var orders = await _context.Orders
                .Include(o => o.Retailer)
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.WholesalerProduct)
                        .ThenInclude(wp => wp.Product)
                            .ThenInclude(p => p.Category)
                .Include(o => o.Transaction)
                    .ThenInclude(t => t.Payments)
                .Where(o => o.WholesalerId == user.Id && 
                        o.OrderDate >= startDate && 
                        o.OrderDate <= endDate)
                .ToListAsync();

            // Customer lifetime value analysis
            var customerAnalysis = orders
                .GroupBy(o => new {
                    CustomerId = o.RetailerId,
                    CustomerName = o.Retailer.BusinessName ?? o.Retailer.UserName
                })
                .Select(g => new {
                    CustomerId = g.Key.CustomerId,
                    CustomerName = g.Key.CustomerName,
                    TotalOrders = g.Count(),
                    TotalRevenue = g.Sum(o => o.TotalAmount),
                    AvgOrderValue = g.Average(o => o.TotalAmount),
                    FirstOrderDate = g.Min(o => o.OrderDate),
                    LastOrderDate = g.Max(o => o.OrderDate),
                    CustomerLifespan = (g.Max(o => o.OrderDate) - g.Min(o => o.OrderDate)).TotalDays,
                    MonthlyFrequency = g.Count() / Math.Max(1, (DateTime.UtcNow - g.Min(o => o.OrderDate)).TotalDays / 30),
                    PaymentReliability = g.Where(o => o.Transaction != null).Sum(o => o.Transaction.AmountPaid) / g.Sum(o => o.TotalAmount) * 100
                })
                .OrderByDescending(c => c.TotalRevenue)
                .ToList();

            // Product performance with inventory insights
            // First, get all WholesalerProducts for this wholesaler
            var wholesalerProducts = await _context.WholesalerProducts
                .Include(wp => wp.Product)
                    .ThenInclude(p => p.Category)
                .Where(wp => wp.WholesalerId == user.Id)
                .ToListAsync();

            // Then get all OrderItems for this wholesaler within the date range
            var wholesalerProductIds = wholesalerProducts.Select(wp => wp.Id).ToList();
            var orderItemsInPeriod = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => wholesalerProductIds.Contains(oi.WholesalerProductId) &&
                           oi.Order.OrderDate >= startDate && 
                           oi.Order.OrderDate <= endDate)
                .ToListAsync();

            // Group order items by WholesalerProductId for analysis
            var orderItemsByProduct = orderItemsInPeriod
                .GroupBy(oi => oi.WholesalerProductId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var productAnalysis = wholesalerProducts
                .Select(wp => {
                    var productOrderItems = orderItemsByProduct.ContainsKey(wp.Id) 
                        ? orderItemsByProduct[wp.Id] 
                        : new List<OrderItem>();
                    
                    return new {
                        ProductId = wp.ProductId,
                        ProductName = wp.Product.Name,
                        CategoryName = wp.Product.Category.Name,
                        CurrentStock = wp.StockQuantity,
                        CurrentPrice = wp.Price,
                        IsActive = wp.IsActive,
                        TotalSold = productOrderItems.Sum(oi => oi.Quantity),
                        TotalRevenue = productOrderItems.Sum(oi => oi.UnitPrice * oi.Quantity),
                        OrderCount = productOrderItems.Select(oi => oi.OrderId).Distinct().Count(),
                        AvgSellPrice = productOrderItems.Any() ? productOrderItems.Average(oi => oi.UnitPrice) : 0,
                        StockTurnover = wp.StockQuantity > 0 && productOrderItems.Any() ? 
                            productOrderItems.Sum(oi => oi.Quantity) / (decimal)wp.StockQuantity : 0,
                        DaysToStockout = productOrderItems.Any() && wp.StockQuantity > 0 ? 
                            wp.StockQuantity / (productOrderItems.Sum(oi => oi.Quantity) / Math.Max(1, (endDate.Value - startDate.Value).TotalDays)) : 0
                    };
                })
                .OrderByDescending(p => p.TotalRevenue)
                .ToList();

            // Market trend analysis
            var marketTrends = orders
                .SelectMany(o => o.Items)
                .GroupBy(oi => new { 
                    Year = oi.Order.OrderDate.Year, 
                    Month = oi.Order.OrderDate.Month,
                    Category = oi.WholesalerProduct.Product.Category.Name
                })
                .Select(g => new {
                    Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Category = g.Key.Category,
                    Revenue = g.Sum(oi => oi.UnitPrice * oi.Quantity),
                    Quantity = g.Sum(oi => oi.Quantity),
                    AvgPrice = g.Average(oi => oi.UnitPrice)
                })
                .OrderBy(t => t.Period)
                .ToList();

            // Seasonal patterns
            var seasonalPatterns = orders
                .GroupBy(o => o.OrderDate.Month)
                .Select(g => new {
                    Month = g.Key,
                    MonthName = new DateTime(2000, g.Key, 1).ToString("MMMM"),
                    TotalOrders = g.Count(),
                    TotalRevenue = g.Sum(o => o.TotalAmount),
                    AvgOrderValue = g.Average(o => o.TotalAmount)
                })
                .OrderBy(s => s.Month)
                .ToList();

            var viewModel = new WholesalerBusinessInsightsViewModel
            {
                StartDate = startDate.Value,
                EndDate = endDate.Value,
                
                CustomerAnalysis = customerAnalysis.Select(c => new CustomerAnalysisItem
                {
                    CustomerId = c.CustomerId,
                    CustomerName = c.CustomerName,
                    TotalOrders = c.TotalOrders,
                    TotalRevenue = c.TotalRevenue,
                    AvgOrderValue = (decimal)c.AvgOrderValue,
                    CustomerLifespan = (int)c.CustomerLifespan,
                    MonthlyFrequency = (decimal)c.MonthlyFrequency,
                    PaymentReliability = (decimal)c.PaymentReliability,
                    LastOrderDate = c.LastOrderDate
                }).Take(15).ToList(),
                
                ProductAnalysis = productAnalysis.Select(p => new WholesalerProductAnalysisItem
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    CategoryName = p.CategoryName,
                    CurrentStock = p.CurrentStock,
                    CurrentPrice = p.CurrentPrice,
                    IsActive = p.IsActive,
                    TotalSold = p.TotalSold,
                    TotalRevenue = p.TotalRevenue,
                    OrderCount = p.OrderCount,
                    AvgSellPrice = (decimal)p.AvgSellPrice,
                    StockTurnover = p.StockTurnover,
                    DaysToStockout = (int)p.DaysToStockout
                }).Take(20).ToList(),
                
                SeasonalPatterns = seasonalPatterns.Select(s => new SeasonalPatternItem
                {
                    Month = s.Month,
                    MonthName = s.MonthName,
                    TotalOrders = s.TotalOrders,
                    TotalRevenue = s.TotalRevenue,
                    AvgOrderValue = (decimal)s.AvgOrderValue
                }).ToList(),
                
                MarketTrends = marketTrends.GroupBy(t => t.Category)
                    .Select(g => new MarketTrendItem
                    {
                        Category = g.Key,
                        TrendData = g.Select(t => new TrendDataPoint
                        {
                            Period = t.Period,
                            Revenue = t.Revenue,
                            Quantity = t.Quantity,
                            AvgPrice = (decimal)t.AvgPrice
                        }).ToList()
                    }).ToList()
            };

            return View(viewModel);
        }

        // GET: Reports/RetailerProfitabilityAnalysis - ROI and profitability insights for Retailers
        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> RetailerProfitabilityAnalysis(DateTime? startDate, DateTime? endDate)
        {
            startDate ??= DateTime.UtcNow.AddMonths(-6);
            endDate ??= DateTime.UtcNow;

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // This would ideally integrate with retailer's own sales data
            // For now, we'll analyze purchasing patterns and suggest markup strategies
            
            var orderItems = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.WholesalerProduct)
                    .ThenInclude(wp => wp.Product)
                        .ThenInclude(p => p.Category)
                .Where(oi => oi.Order.RetailerId == user.Id && 
                        oi.Order.OrderDate >= startDate && 
                        oi.Order.OrderDate <= endDate)
                .ToListAsync();

            // Cost analysis by category
            var categoryProfitability = orderItems
                .GroupBy(oi => oi.WholesalerProduct.Product.Category.Name)
                .Select(g => new {
                    Category = g.Key,
                    TotalCost = g.Sum(oi => oi.UnitPrice * oi.Quantity),
                    TotalQuantity = g.Sum(oi => oi.Quantity),
                    AvgCostPerUnit = g.Average(oi => oi.UnitPrice),
                    ProductCount = g.Select(oi => oi.WholesalerProduct.ProductId).Distinct().Count(),
                    SuggestedMarkup = 0.4m, // 40% suggested markup
                    PotentialRevenue = g.Sum(oi => oi.UnitPrice * oi.Quantity) * 1.4m
                })
                .OrderByDescending(c => c.TotalCost)
                .ToList();

            var viewModel = new RetailerProfitabilityAnalysisViewModel
            {
                StartDate = startDate.Value,
                EndDate = endDate.Value,
                CategoryProfitability = categoryProfitability.Select(c => new CategoryProfitabilityItem
                {
                    Category = c.Category,
                    TotalCost = c.TotalCost,
                    TotalQuantity = c.TotalQuantity,
                    AvgCostPerUnit = (decimal)c.AvgCostPerUnit,
                    ProductCount = c.ProductCount,
                    SuggestedMarkup = c.SuggestedMarkup,
                    PotentialRevenue = c.PotentialRevenue,
                    PotentialProfit = c.PotentialRevenue - c.TotalCost
                }).ToList()
            };

            return View(viewModel);
        }

        // GET: Reports/UpdatePaymentStatuses - Admin only action to fix payment statuses
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePaymentStatuses()
        {
            // Get all transactions
            var transactions = await _context.Transactions.ToListAsync();
            int updatedCount = 0;
            
            foreach (var transaction in transactions)
            {
                // Check if payment status needs to be updated
                if (transaction.AmountPaid >= transaction.Amount && transaction.Status != TransactionStatus.Completed)
                {
                    transaction.Status = TransactionStatus.Completed;
                    _context.Update(transaction);
                    updatedCount++;
                }
                else if (transaction.AmountPaid > 0 && transaction.AmountPaid < transaction.Amount && 
                         transaction.Status != TransactionStatus.PartiallyPaid)
                {
                    transaction.Status = TransactionStatus.PartiallyPaid;
                    _context.Update(transaction);
                    updatedCount++;
                }
            }
            
            await _context.SaveChangesAsync();
            
            TempData["Success"] = $"Updated payment status for {updatedCount} transactions.";
            return RedirectToAction(nameof(Index));
        }

        // Helper methods for enhanced performance reports
        private async Task<List<CategorySpendingItem>> GetCategorySpendingAnalysis(string retailerId, DateTime startDate, DateTime endDate)
        {
            var orderItems = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.WholesalerProduct)
                    .ThenInclude(wp => wp.Product)
                        .ThenInclude(p => p.Category)
                .Where(oi => oi.Order.RetailerId == retailerId &&
                           oi.Order.OrderDate >= startDate &&
                           oi.Order.OrderDate <= endDate &&
                           oi.Order.Status != OrderStatus.Cancelled) // Exclude cancelled orders
                .ToListAsync();

            var totalSpent = orderItems.Sum(oi => oi.UnitPrice * oi.Quantity);

            return orderItems
                .GroupBy(oi => oi.WholesalerProduct.Product.Category.Name)
                .Select(g => new CategorySpendingItem
                {
                    CategoryName = g.Key,
                    TotalSpent = g.Sum(oi => oi.UnitPrice * oi.Quantity),
                    OrderCount = g.Select(oi => oi.OrderId).Distinct().Count(),
                    ProductCount = g.Select(oi => oi.WholesalerProduct.ProductId).Distinct().Count(),
                    AvgOrderValue = g.GroupBy(oi => oi.OrderId).Average(og => og.Sum(oi => oi.UnitPrice * oi.Quantity)),
                    SpendingPercentage = totalSpent > 0 ? (g.Sum(oi => oi.UnitPrice * oi.Quantity) / totalSpent) * 100 : 0
                })
                .OrderByDescending(c => c.TotalSpent)
                .ToList();
        }

        private async Task<List<MonthlySpendingTrendItem>> GetMonthlySpendingTrends(string retailerId, DateTime startDate, DateTime endDate)
        {
            var orders = await _context.Orders
                .Include(o => o.Transaction)
                    .ThenInclude(t => t.Payments)
                .Where(o => o.RetailerId == retailerId &&
                           o.OrderDate >= startDate &&
                           o.OrderDate <= endDate &&
                           o.Status != OrderStatus.Cancelled) // Exclude cancelled orders
                .ToListAsync();

            var monthlyData = orders
                .GroupBy(o => new { Year = o.OrderDate.Year, Month = o.OrderDate.Month })
                .Select(g => new MonthlySpendingTrendItem
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    AmountSpent = g.Where(o => o.Transaction != null).Sum(o => o.Transaction.AmountPaid),
                    OrderCount = g.Count(),
                    AvgOrderValue = g.Average(o => o.TotalAmount),
                    GrowthRate = 0 // Will be calculated below
                })
                .OrderBy(m => m.Month)
                .ToList();

            // Calculate growth rates
            for (int i = 1; i < monthlyData.Count; i++)
            {
                var current = monthlyData[i].AmountSpent;
                var previous = monthlyData[i - 1].AmountSpent;
                monthlyData[i].GrowthRate = previous > 0 ? ((current - previous) / previous) * 100 : 0;
            }

            return monthlyData;
        }

        private decimal CalculateGrowthRate(List<MonthlySpendingTrendItem> trends)
        {
            if (trends.Count < 2) return 0;
            
            var latestMonth = trends.LastOrDefault();
            var previousMonth = trends.Count > 1 ? trends[trends.Count - 2] : null;
            
            if (latestMonth == null || previousMonth == null || previousMonth.AmountSpent == 0)
                return 0;
                
            return ((latestMonth.AmountSpent - previousMonth.AmountSpent) / previousMonth.AmountSpent) * 100;
        }

        private async Task<List<CategoryPerformanceItem>> GetCategoryPerformanceAnalysis(string wholesalerId, DateTime startDate, DateTime endDate)
        {
            var orderItems = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.WholesalerProduct)
                    .ThenInclude(wp => wp.Product)
                        .ThenInclude(p => p.Category)
                .Where(oi => oi.WholesalerProduct.WholesalerId == wholesalerId &&
                           oi.Order.OrderDate >= startDate &&
                           oi.Order.OrderDate <= endDate &&
                           oi.Order.Status != OrderStatus.Cancelled) // Exclude cancelled orders
                .ToListAsync();

            var totalRevenue = orderItems.Sum(oi => oi.UnitPrice * oi.Quantity);

            return orderItems
                .GroupBy(oi => oi.WholesalerProduct.Product.Category.Name)
                .Select(g => new CategoryPerformanceItem
                {
                    CategoryName = g.Key,
                    TotalRevenue = g.Sum(oi => oi.UnitPrice * oi.Quantity),
                    QuantitySold = g.Sum(oi => oi.Quantity),
                    OrderCount = g.Select(oi => oi.OrderId).Distinct().Count(),
                    AvgPrice = g.Average(oi => oi.UnitPrice),
                    RevenuePercentage = totalRevenue > 0 ? (g.Sum(oi => oi.UnitPrice * oi.Quantity) / totalRevenue) * 100 : 0,
                    PerformanceTrend = "Stable" // This could be enhanced with historical comparison
                })
                .OrderByDescending(c => c.TotalRevenue)
                .ToList();
        }

        private async Task<List<MonthlyRevenueTrendItem>> GetMonthlyRevenueTrends(string wholesalerId, DateTime startDate, DateTime endDate)
        {
            var orders = await _context.Orders
                .Include(o => o.Transaction)
                    .ThenInclude(t => t.Payments)
                .Where(o => o.WholesalerId == wholesalerId &&
                           o.OrderDate >= startDate &&
                           o.OrderDate <= endDate &&
                           o.Status != OrderStatus.Cancelled) // Exclude cancelled orders
                .ToListAsync();

            var monthlyData = orders
                .GroupBy(o => new { Year = o.OrderDate.Year, Month = o.OrderDate.Month })
                .Select(g => new MonthlyRevenueTrendItem
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Revenue = g.Where(o => o.Transaction != null).Sum(o => o.Transaction.AmountPaid),
                    OrderCount = g.Count(),
                    AvgOrderValue = g.Average(o => o.TotalAmount),
                    GrowthRate = 0 // Will be calculated below
                })
                .OrderBy(m => m.Month)
                .ToList();

            // Calculate growth rates
            for (int i = 1; i < monthlyData.Count; i++)
            {
                var current = monthlyData[i].Revenue;
                var previous = monthlyData[i - 1].Revenue;
                monthlyData[i].GrowthRate = previous > 0 ? ((current - previous) / previous) * 100 : 0;
            }

            return monthlyData;
        }

        private async Task<List<InventoryStatusItem>> GetInventoryStatusAnalysis(string wholesalerId, DateTime startDate, DateTime endDate)
        {
            var wholesalerProducts = await _context.WholesalerProducts
                .Include(wp => wp.Product)
                    .ThenInclude(p => p.Category)
                .Where(wp => wp.WholesalerId == wholesalerId)
                .ToListAsync();

            var productIds = wholesalerProducts.Select(wp => wp.Id).ToList();
            var orderItems = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => productIds.Contains(oi.WholesalerProductId) &&
                           oi.Order.OrderDate >= startDate &&
                           oi.Order.OrderDate <= endDate &&
                           oi.Order.Status != OrderStatus.Cancelled) // Exclude cancelled orders
                .ToListAsync();

            var orderItemsByProduct = orderItems
                .GroupBy(oi => oi.WholesalerProductId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return wholesalerProducts
                .Select(wp => {
                    var productOrderItems = orderItemsByProduct.ContainsKey(wp.Id) 
                        ? orderItemsByProduct[wp.Id] 
                        : new List<OrderItem>();
                    
                    var quantitySold = productOrderItems.Sum(oi => oi.Quantity);
                    var revenue = productOrderItems.Sum(oi => oi.UnitPrice * oi.Quantity);
                    var stockTurnover = wp.StockQuantity > 0 ? (decimal)quantitySold / wp.StockQuantity : 0;
                    
                    return new InventoryStatusItem
                    {
                        ProductName = wp.Product.Name,
                        CategoryName = wp.Product.Category.Name,
                        CurrentStock = wp.StockQuantity,
                        QuantitySold = quantitySold,
                        Revenue = revenue,
                        StockStatus = wp.StockQuantity <= 10 ? "Critical" :
                                    wp.StockQuantity <= 50 ? "Low" :
                                    wp.StockQuantity <= 200 ? "Normal" : "High",
                        StockTurnover = stockTurnover
                    };
                })
                .OrderByDescending(i => i.Revenue)
                .Take(20)
                .ToList();
        }

        private decimal CalculateRevenueGrowthRate(List<MonthlyRevenueTrendItem> trends)
        {
            if (trends.Count < 2) return 0;
            
            var latestMonth = trends.LastOrDefault();
            var previousMonth = trends.Count > 1 ? trends[trends.Count - 2] : null;
            
            if (latestMonth == null || previousMonth == null || previousMonth.Revenue == 0)
                return 0;
                
            return ((latestMonth.Revenue - previousMonth.Revenue) / previousMonth.Revenue) * 100;
        }

        // GET: Reports/WholesalerPayments - Detailed payment and revenue report for wholesalers
        [Authorize(Roles = "Wholesaler")]
        public async Task<IActionResult> WholesalerPayments(DateTime? startDate, DateTime? endDate, string retailerId = null, string paymentStatus = null)
        {
            // Default to current month if no dates provided
            startDate ??= new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            endDate ??= DateTime.UtcNow;

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Get all retailers that have ordered from this wholesaler for filter dropdown
            var retailerIds = await _context.Orders
                .Where(o => o.WholesalerId == user.Id)
                .Select(o => o.RetailerId)
                .Distinct()
                .ToListAsync();

            var retailers = await _context.Users
                .Where(u => retailerIds.Contains(u.Id))
                .OrderBy(u => u.BusinessName)
                .ToListAsync();

            // Build base query for orders from this wholesaler (exclude cancelled orders)
            var ordersQuery = _context.Orders
                .Include(o => o.Retailer)
                .Include(o => o.Transaction)
                    .ThenInclude(t => t.Payments)
                .Include(o => o.Items)
                    .ThenInclude(i => i.WholesalerProduct)
                        .ThenInclude(wp => wp.Product)
                .Where(o => o.WholesalerId == user.Id && 
                           o.OrderDate >= startDate && 
                           o.OrderDate <= endDate &&
                           o.Status != OrderStatus.Cancelled); // Exclude cancelled orders

            // Apply retailer filter if provided
            if (!string.IsNullOrEmpty(retailerId))
            {
                ordersQuery = ordersQuery.Where(o => o.RetailerId == retailerId);
            }

            var orders = await ordersQuery
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Ensure all orders have transactions - create missing ones
            foreach (var order in orders)
            {
                if (order.Transaction == null)
                {
                    var newTransaction = new Transaction
                    {
                        OrderId = order.Id,
                        Amount = order.TotalAmount,
                        AmountPaid = 0,
                        TransactionDate = order.OrderDate,
                        Payments = new List<Payment>()
                    };
                    _context.Transactions.Add(newTransaction);
                    order.Transaction = newTransaction;
                }
            }
            await _context.SaveChangesAsync();

            // Get all transactions for these orders with current payment data
            var orderIds = orders.Select(o => o.Id).ToList();
            var transactions = await _context.Transactions
                .Include(t => t.Payments)
                .Where(t => orderIds.Contains(t.OrderId))
                .ToListAsync();

            // Create lookup dictionary - handle potential duplicates by taking the first transaction per order
            var transactionLookup = transactions
                .GroupBy(t => t.OrderId)
                .ToDictionary(g => g.Key, g => g.First());

            // Update AmountPaid for all transactions first (to ensure accuracy)
            foreach (var transaction in transactions)
            {
                var actualAmountPaid = transaction.Payments?.Sum(p => p.Amount) ?? 0;
                if (transaction.AmountPaid != actualAmountPaid)
                {
                    transaction.AmountPaid = actualAmountPaid;
                    _context.Update(transaction);
                }
            }
            await _context.SaveChangesAsync();

            // Update the orders' Transaction objects with the corrected AmountPaid values
            foreach (var order in orders)
            {
                if (transactionLookup.ContainsKey(order.Id))
                {
                    order.Transaction = transactionLookup[order.Id];
                }
            }

            // Apply payment status filter
            if (!string.IsNullOrEmpty(paymentStatus))
            {
                orders = orders.Where(o => 
                {
                    if (!transactionLookup.ContainsKey(o.Id)) return paymentStatus == "unpaid";
                    var transaction = transactionLookup[o.Id];
                    var actualPaid = transaction.Payments?.Sum(p => p.Amount) ?? 0;
                    return paymentStatus switch
                    {
                        "paid" => actualPaid >= transaction.Amount,
                        "partial" => actualPaid > 0 && actualPaid < transaction.Amount,
                        "unpaid" => actualPaid == 0,
                        "overdue" => actualPaid < transaction.Amount && 
                                    (DateTime.UtcNow - transaction.TransactionDate).TotalDays > 30,
                        _ => true
                    };
                }).ToList();
            }

            // Calculate payment summary metrics using actual payments
            var totalOrderValue = orders.Sum(o => o.TotalAmount);
            var totalReceived = orders.Where(o => transactionLookup.ContainsKey(o.Id))
                                     .Sum(o => {
                                         var transaction = transactionLookup[o.Id];
                                         return transaction.Payments?.Sum(p => p.Amount) ?? 0;
                                     });
            var totalOutstanding = totalOrderValue - totalReceived;

            var fullyPaidOrders = orders.Count(o => {
                if (!transactionLookup.ContainsKey(o.Id)) return false;
                var transaction = transactionLookup[o.Id];
                var actualPaid = transaction.Payments?.Sum(p => p.Amount) ?? 0;
                return actualPaid >= o.TotalAmount;
            });
            
            var partiallyPaidOrders = orders.Count(o => {
                if (!transactionLookup.ContainsKey(o.Id)) return false;
                var transaction = transactionLookup[o.Id];
                var actualPaid = transaction.Payments?.Sum(p => p.Amount) ?? 0;
                return actualPaid > 0 && actualPaid < o.TotalAmount;
            });
            
            var unpaidOrders = orders.Count(o => {
                if (!transactionLookup.ContainsKey(o.Id)) return true;
                var transaction = transactionLookup[o.Id];
                var actualPaid = transaction.Payments?.Sum(p => p.Amount) ?? 0;
                return actualPaid == 0;
            });

            // Calculate overdue payments (over 30 days)
            var overdueOrders = orders.Where(o => {
                if (!transactionLookup.ContainsKey(o.Id)) return false;
                var transaction = transactionLookup[o.Id];
                var actualPaid = transaction.Payments?.Sum(p => p.Amount) ?? 0;
                var outstanding = o.TotalAmount - actualPaid;
                var daysSinceOrder = (DateTime.UtcNow - o.OrderDate).TotalDays;
                return outstanding > 0 && daysSinceOrder > 30;
            }).ToList();
            
            var overdueAmount = overdueOrders.Sum(o => {
                var transaction = transactionLookup[o.Id];
                var actualPaid = transaction.Payments?.Sum(p => p.Amount) ?? 0;
                return o.TotalAmount - actualPaid;
            });

            // Payment collection rate
            var collectionRate = totalOrderValue > 0 ? (totalReceived / totalOrderValue) * 100 : 0;

            // Average days to payment
            var paymentsWithDates = orders.Where(o => transactionLookup.ContainsKey(o.Id) && 
                                                 transactionLookup[o.Id].Payments.Any())
                                          .SelectMany(o => transactionLookup[o.Id].Payments
                                                          .Select(p => (DateTime.UtcNow - o.OrderDate).TotalDays))
                                          .ToList();
            var avgDaysToPayment = paymentsWithDates.Any() ? paymentsWithDates.Average() : 0;

            // Monthly revenue trends
            var monthlyTrends = orders.GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new PaymentMonthlyTrendItem
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    OrderCount = g.Count(),
                    TotalOrdered = g.Sum(o => o.TotalAmount),
                    TotalReceived = g.Where(o => transactionLookup.ContainsKey(o.Id))
                                   .Sum(o => {
                                       var transaction = transactionLookup[o.Id];
                                       return transaction.Payments?.Sum(p => p.Amount) ?? 0;
                                   })
                })
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToList();

            // Payment method analysis
            var paymentMethods = orders.Where(o => transactionLookup.ContainsKey(o.Id))
                .SelectMany(o => transactionLookup[o.Id].Payments)
                .GroupBy(p => p.Method)
                .Select(g => new PaymentMethodItem
                {
                    Method = g.Key.ToString(),
                    Count = g.Count(),
                    Amount = g.Sum(p => p.Amount),
                    Percentage = totalReceived > 0 ? (g.Sum(p => p.Amount) / totalReceived) * 100 : 0
                })
                .OrderByDescending(p => p.Amount)
                .ToList();

            // Recent payments (last 10)
            var recentPayments = orders.Where(o => transactionLookup.ContainsKey(o.Id))
                .SelectMany(o => transactionLookup[o.Id].Payments.Select(p => new RecentPaymentItem
                {
                    Payment = p,
                    Order = o,
                    Retailer = o.Retailer
                }))
                .OrderByDescending(x => x.Payment.PaymentDate)
                .Take(10)
                .ToList();

            // Aging analysis (outstanding amounts by days)
            var agingAnalysis = orders.Where(o => {
                    if (!transactionLookup.ContainsKey(o.Id)) return false;
                    var transaction = transactionLookup[o.Id];
                    var actualPaid = transaction.Payments?.Sum(p => p.Amount) ?? 0;
                    return actualPaid < o.TotalAmount;
                })
                .GroupBy(o => 
                {
                    var days = (DateTime.UtcNow - o.OrderDate).Days;
                    return days switch
                    {
                        <= 30 => "0-30 days",
                        <= 60 => "31-60 days",
                        <= 90 => "61-90 days",
                        _ => "90+ days"
                    };
                })
                .Select(g => new AgingAnalysisItem
                {
                    AgeRange = g.Key,
                    Count = g.Count(),
                    Amount = g.Sum(o => {
                        var transaction = transactionLookup[o.Id];
                        var actualPaid = transaction.Payments?.Sum(p => p.Amount) ?? 0;
                        return o.TotalAmount - actualPaid;
                    })
                })
                .ToList();

            var viewModel = new WholesalerPaymentsReportViewModel
            {
                StartDate = startDate.Value,
                EndDate = endDate.Value,
                RetailerId = retailerId,
                PaymentStatus = paymentStatus,
                WholesalerId = user.Id,
                WholesalerName = user.BusinessName ?? user.UserName,

                // Summary Metrics
                TotalOrders = orders.Count,
                TotalOrderValue = totalOrderValue,
                TotalReceived = totalReceived,
                TotalOutstanding = totalOutstanding,
                CollectionRate = collectionRate,
                AvgDaysToPayment = avgDaysToPayment,

                // Payment Status Counts
                FullyPaidOrders = fullyPaidOrders,
                PartiallyPaidOrders = partiallyPaidOrders,
                UnpaidOrders = unpaidOrders,
                OverdueOrders = overdueOrders.Count,
                OverdueAmount = overdueAmount,

                // Collections
                Orders = orders,
                Retailers = retailers,
                RevenueByRetailer = new List<RetailerRevenueItem>(), // Removed as per user request
                MonthlyTrends = monthlyTrends,
                PaymentMethods = paymentMethods,
                RecentPayments = recentPayments,
                AgingAnalysis = agingAnalysis
            };

            return View(viewModel);
        }
    }
} 
