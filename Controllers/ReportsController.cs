using DFTRK.Data;
using DFTRK.Models;
using DFTRK.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        // Main reports index
        public IActionResult Index()
        {
            return View();
        }

        // Wholesaler reports index
        [Authorize(Roles = "Wholesaler")]
        public IActionResult WholesalerIndex()
        {
            return RedirectToAction("WholesalerSalesOutstanding");
        }

        // Simple wholesaler sales and outstanding report
        [Authorize(Roles = "Wholesaler")]
        public async Task<IActionResult> WholesalerSalesOutstanding(DateTime? startDate, DateTime? endDate, string? customerFilter = null)
        {
            startDate ??= new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            endDate ??= DateTime.UtcNow.Date.AddDays(1); // End of today (start of tomorrow)

            // Ensure we get the full day for user-selected end dates
            if (endDate.HasValue && endDate.Value.TimeOfDay == TimeSpan.Zero)
            {
                endDate = endDate.Value.AddDays(1); // Include entire end date
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var allOrders = await _context.Orders
                .Include(o => o.Retailer)
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.WholesalerProduct)
                        .ThenInclude(wp => wp.Product)
                            .ThenInclude(p => p.Category)
                .Where(o => o.WholesalerId == user.Id && 
                           o.OrderDate >= startDate && 
                           o.OrderDate < endDate &&  // Use < instead of <= for cleaner boundary logic
                           o.Status != OrderStatus.Cancelled)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var orderIds = allOrders.Select(o => o.Id).ToList();
            var transactions = await _context.Transactions
                .Include(t => t.Payments)
                .Where(t => orderIds.Contains(t.OrderId))
                .ToListAsync();

            var transactionLookup = transactions.GroupBy(t => t.OrderId).ToDictionary(g => g.Key, g => g.First());

            // Apply customer filter after loading (to handle both retailer IDs and external customer names)
            var orders = allOrders;
            if (!string.IsNullOrEmpty(customerFilter))
            {
                if (customerFilter.StartsWith("EXTERNAL:"))
                {
                    // External customer filter: extract customer name and filter by external orders
                    var customerName = customerFilter.Substring("EXTERNAL:".Length);
                    orders = allOrders.Where(o => o.RetailerId == null && 
                                             o.Notes != null && 
                                             o.Notes.StartsWith($"External Order for: {customerName}"))
                                   .ToList();
                }
                else
                {
                    // Registered retailer filter: filter by RetailerId
                    orders = allOrders.Where(o => o.RetailerId == customerFilter).ToList();
                }
            }

            // Build customer list (both registered retailers and external customers)
            var customers = new List<dynamic>();

            // Add registered retailers
            var retailerIds = allOrders
                .Where(o => o.RetailerId != null)
                .Select(o => o.RetailerId!)
                .Distinct()
                .ToList();

            var retailers = await _context.Users
                .Where(u => retailerIds.Contains(u.Id))
                .ToListAsync();

            customers.AddRange(retailers.Select(r => new { 
                Id = r.Id, 
                Name = r.BusinessName ?? r.UserName ?? "Unknown",
                Type = "Retailer"
            }));

            // Add external customers (extract from external orders)
            var externalOrders = allOrders
                .Where(o => o.RetailerId == null && 
                           o.Notes != null &&
                           o.Notes.StartsWith("External Order for: "))
                .ToList();

            var externalCustomerNames = externalOrders
                .Select(o => o.Notes!.Split('\n')[0].Replace("External Order for: ", "").Trim())
                .Distinct()
                .ToList();

            customers.AddRange(externalCustomerNames.Select(name => new { 
                Id = $"EXTERNAL:{name}", 
                Name = name,
                Type = "External"
            }));

            // Sort customers by type then by name
            var availableCustomers = customers.OrderBy(c => c.Type).ThenBy(c => c.Name).ToList();

            var totalSales = orders.Sum(o => o.TotalAmount);
            var totalPaid = orders.Where(o => transactionLookup.ContainsKey(o.Id))
                                 .Sum(o => transactionLookup[o.Id].AmountPaid);
            var totalOutstanding = totalSales - totalPaid;
            var paidOrders = orders.Count(o => transactionLookup.ContainsKey(o.Id) && 
                                              transactionLookup[o.Id].AmountPaid >= o.TotalAmount);
            var unpaidOrders = orders.Count - paidOrders;

            // Calculate top products
            var topProducts = orders
                .SelectMany(o => o.Items ?? new List<OrderItem>())
                .Where(i => i.WholesalerProduct?.Product != null)
                .GroupBy(i => new { 
                    ProductId = i.WholesalerProduct.Product.Id, 
                    ProductName = i.WholesalerProduct.Product.Name,
                    CategoryName = i.WholesalerProduct.Product.Category?.Name ?? "Uncategorized"
                })
                .Select(g => new WholesalerTopProduct
                {
                    ProductName = g.Key.ProductName,
                    CategoryName = g.Key.CategoryName,
                    QuantitySold = g.Sum(i => i.Quantity),
                    Revenue = g.Sum(i => i.Subtotal),
                    OrderCount = g.Select(i => i.OrderId).Distinct().Count()
                })
                .OrderByDescending(p => p.Revenue)
                .Take(5)
                .ToList();

            var sales = orders.Select(o => {
                var transaction = transactionLookup.ContainsKey(o.Id) ? transactionLookup[o.Id] : null;
                var amountPaid = transaction?.AmountPaid ?? 0;
                var outstanding = o.TotalAmount - amountPaid;
                
                return new WholesalerSaleItem
                {
                    OrderId = o.Id,
                    OrderDate = o.OrderDate,
                    CustomerName = o.RetailerId != null ? 
                        (o.Retailer?.BusinessName ?? o.Retailer?.UserName ?? "Unknown") :
                        (o.Notes?.StartsWith("External Order for: ") == true ?
                            o.Notes.Split('\n')[0].Replace("External Order for: ", "").Trim() :
                            "External Customer"),
                    OrderTotal = o.TotalAmount,
                    AmountPaid = amountPaid,
                    Outstanding = outstanding,
                    Status = o.Status,
                    PaymentStatus = outstanding <= 0 ? "Paid" : 
                                   amountPaid > 0 ? "Partial" : "Unpaid",
                    IsExternal = o.RetailerId == null
                };
            }).ToList();

            var viewModel = new WholesalerSalesOutstandingViewModel
            {
                StartDate = startDate.Value,
                EndDate = endDate.Value,
                RetailerFilter = customerFilter,  // Keep same property name for backwards compatibility
                TotalSales = totalSales,
                TotalPaid = totalPaid,
                TotalOutstanding = totalOutstanding,
                TotalOrders = orders.Count,
                PaidOrders = paidOrders,
                UnpaidOrders = unpaidOrders,
                Sales = sales,
                AvailableRetailers = availableCustomers,  // Now includes both retailers and external customers
                TopProducts = topProducts
            };

            return View(viewModel);
        }

        // Retailer reports index
        [Authorize(Roles = "Retailer")]
        public IActionResult RetailerIndex()
        {
            return RedirectToAction("RetailerPurchases");
        }

        // Enhanced financial report with retailer/wholesaler filtering
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Financial(DateTime? startDate, DateTime? endDate, string? retailerFilter, string? wholesalerFilter)
        {
            startDate ??= new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            endDate ??= DateTime.UtcNow.Date.AddDays(1); // End of today (start of tomorrow)

            // Ensure we get the full day for user-selected end dates
            if (endDate.HasValue && endDate.Value.TimeOfDay == TimeSpan.Zero)
            {
                endDate = endDate.Value.AddDays(1); // Include entire end date
            }

            var allTransactions = await _context.Transactions
                .Include(t => t.Order)
                    .ThenInclude(o => o.Retailer)
                .Include(t => t.Order)
                    .ThenInclude(o => o.Wholesaler)
                .Include(t => t.Payments)
                .Where(t => t.Order.OrderDate >= startDate && t.Order.OrderDate < endDate &&
                           t.Order.Status != OrderStatus.Cancelled)
                .ToListAsync();

            // Apply filters
            var transactions = allTransactions;
            if (!string.IsNullOrEmpty(retailerFilter))
            {
                if (retailerFilter.StartsWith("EXTERNAL:"))
                {
                    var externalName = retailerFilter.Substring("EXTERNAL:".Length);
                    transactions = transactions.Where(t => t.Order.RetailerId == null && 
                                                         t.Order.Notes != null && 
                                                         t.Order.Notes.Contains(externalName)).ToList();
                }
                else
                {
                    transactions = transactions.Where(t => t.Order.RetailerId == retailerFilter).ToList();
                }
            }

            if (!string.IsNullOrEmpty(wholesalerFilter))
            {
                if (wholesalerFilter.StartsWith("PARTNER:"))
                {
                    var partnerName = wholesalerFilter.Substring("PARTNER:".Length);
                    transactions = transactions.Where(t => t.Order.WholesalerId == null && 
                                                         t.Order.Notes != null && 
                                                         t.Order.Notes.Contains(partnerName)).ToList();
                }
                else
                {
                    transactions = transactions.Where(t => t.Order.WholesalerId == wholesalerFilter).ToList();
                }
            }

            // ALL transactions generate platform fees since all involve registered users
            // (Partners are wholesalers, External customers are retailers)
            var totalTransactionVolume = transactions.Sum(t => t.Amount);
            var actualRevenue = transactions.Sum(t => t.Payments?.Sum(p => p.Amount) ?? 0);
            var outstandingAmount = totalTransactionVolume - actualRevenue;
            
            // Platform fees charged on ALL orders (0.1% fee rate)
            var platformFees = totalTransactionVolume * 0.001m;
            var collectionRate = totalTransactionVolume > 0 ? (actualRevenue / totalTransactionVolume) * 100 : 0;

            // Calculate orders this month
            var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var nextMonth = currentMonth.AddMonths(1);
            
            var ordersThisMonth = await _context.Orders
                .Where(o => o.OrderDate >= currentMonth && o.OrderDate < nextMonth &&
                           o.Status != OrderStatus.Cancelled)
                .CountAsync();

            var monthlyData = transactions
                .GroupBy(t => new { t.Order.OrderDate.Year, t.Order.OrderDate.Month })
                .Select(g => new MonthlyFinancialData
                {
                    Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                    TransactionVolume = g.Sum(t => t.Amount),
                    ActualRevenue = g.Sum(t => t.Payments?.Sum(p => p.Amount) ?? 0),
                    // Platform fees charged on ALL orders (0.1% fee rate)
                    PlatformFees = g.Sum(t => t.Amount) * 0.001m,
                    TransactionCount = g.Count()
                })
                .OrderBy(m => m.Period)
                .ToList();

            // Retailer breakdowns
            var retailerBreakdowns = new List<RetailerFinancialBreakdown>();
            
            // All retailer transactions (including registered retailers and external customers)
            // 1. Registered retailers
            var registeredRetailerTransactions = transactions.Where(t => t.Order.Retailer != null).ToList();
            var registeredRetailerGroups = registeredRetailerTransactions
                .GroupBy(t => new { t.Order.RetailerId, t.Order.Retailer!.BusinessName })
                .ToList();

            foreach (var group in registeredRetailerGroups)
            {
                var groupTransactions = group.ToList();
                var volume = groupTransactions.Sum(t => t.Amount);
                var paid = groupTransactions.Sum(t => t.Payments?.Sum(p => p.Amount) ?? 0);
                
                retailerBreakdowns.Add(new RetailerFinancialBreakdown
                {
                    RetailerId = group.Key.RetailerId!,
                    RetailerName = group.Key.BusinessName ?? "Unknown Retailer",
                    OrderCount = groupTransactions.Select(t => t.OrderId).Distinct().Count(),
                    TransactionVolume = volume,
                    PlatformFees = volume * 0.001m, // Platform fees on ALL orders
                    AmountPaid = paid,
                    Outstanding = volume - paid,
                    CollectionRate = volume > 0 ? (paid / volume) * 100 : 0,
                    Type = "Registered"
                });
            }

            // 2. External customers (they are retailers too, just ordering externally)
            var externalCustomerTransactions = transactions.Where(t => t.Order.RetailerId == null && 
                                                                 t.Order.Notes != null && 
                                                                 t.Order.Notes.StartsWith("External Order for: ")).ToList();
            var externalGroups = externalCustomerTransactions
                .GroupBy(t => ExtractExternalCustomerName(t.Order.Notes!))
                .ToList();

            foreach (var group in externalGroups)
            {
                var groupTransactions = group.ToList();
                var volume = groupTransactions.Sum(t => t.Amount);
                var paid = groupTransactions.Sum(t => t.Payments?.Sum(p => p.Amount) ?? 0);
                
                retailerBreakdowns.Add(new RetailerFinancialBreakdown
                {
                    RetailerId = $"EXTERNAL:{group.Key}",
                    RetailerName = group.Key,
                    OrderCount = groupTransactions.Select(t => t.OrderId).Distinct().Count(),
                    TransactionVolume = volume,
                    PlatformFees = volume * 0.001m, // Platform fees on ALL orders
                    AmountPaid = paid,
                    Outstanding = volume - paid,
                    CollectionRate = volume > 0 ? (paid / volume) * 100 : 0,
                    Type = "External"
                });
            }

            // Wholesaler breakdowns
            var wholesalerBreakdowns = new List<WholesalerFinancialBreakdown>();
            
            // All wholesaler transactions (including registered wholesalers and partners)
            // 1. Registered wholesalers
            var registeredWholesalerTransactions = transactions.Where(t => t.Order.Wholesaler != null).ToList();
            var registeredWholesalerGroups = registeredWholesalerTransactions
                .GroupBy(t => new { t.Order.WholesalerId, t.Order.Wholesaler!.BusinessName })
                .ToList();

            foreach (var group in registeredWholesalerGroups)
            {
                var groupTransactions = group.ToList();
                var volume = groupTransactions.Sum(t => t.Amount);
                var paid = groupTransactions.Sum(t => t.Payments?.Sum(p => p.Amount) ?? 0);
                
                wholesalerBreakdowns.Add(new WholesalerFinancialBreakdown
                {
                    WholesalerId = group.Key.WholesalerId!,
                    WholesalerName = group.Key.BusinessName ?? "Unknown Wholesaler",
                    OrderCount = groupTransactions.Select(t => t.OrderId).Distinct().Count(),
                    TransactionVolume = volume,
                    PlatformFees = volume * 0.001m, // Platform fees on ALL orders
                    AmountPaid = paid,
                    Outstanding = volume - paid,
                    CollectionRate = volume > 0 ? (paid / volume) * 100 : 0,
                    Type = "Registered"
                });
            }

            // 2. Partnership orders (partners are wholesalers too, just working through partnerships)
            var partnershipTransactions = transactions.Where(t => t.Order.WholesalerId == null && 
                                                                   t.Order.Notes != null && 
                                                                   t.Order.Notes.StartsWith("Partnership Order from: ")).ToList();
            var partnershipGroups = partnershipTransactions
                .GroupBy(t => ExtractPartnershipName(t.Order.Notes!))
                .ToList();

            foreach (var group in partnershipGroups)
            {
                var groupTransactions = group.ToList();
                var volume = groupTransactions.Sum(t => t.Amount);
                var paid = groupTransactions.Sum(t => t.Payments?.Sum(p => p.Amount) ?? 0);
                
                wholesalerBreakdowns.Add(new WholesalerFinancialBreakdown
                {
                    WholesalerId = $"PARTNER:{group.Key}",
                    WholesalerName = group.Key,
                    OrderCount = groupTransactions.Select(t => t.OrderId).Distinct().Count(),
                    TransactionVolume = volume,
                    PlatformFees = volume * 0.001m, // Platform fees on ALL orders
                    AmountPaid = paid,
                    Outstanding = volume - paid,
                    CollectionRate = volume > 0 ? (paid / volume) * 100 : 0,
                    Type = "Partnership"
                });
            }

            // Build filter options - only show registered users
            var availableRetailers = retailerBreakdowns
                .Where(r => r.Type == "Registered")
                .Select(r => new { Id = r.RetailerId, Name = r.RetailerName })
                .Cast<dynamic>()
                .ToList();

            var availableWholesalers = wholesalerBreakdowns
                .Where(w => w.Type == "Registered")
                .Select(w => new { Id = w.WholesalerId, Name = w.WholesalerName })
                .Cast<dynamic>()
                .ToList();

            var viewModel = new FinancialReportViewModel
            {
                StartDate = startDate.Value,
                EndDate = endDate.Value,
                RetailerFilter = retailerFilter,
                WholesalerFilter = wholesalerFilter,
                TotalTransactionVolume = totalTransactionVolume,
                ActualRevenue = actualRevenue,
                OutstandingAmount = outstandingAmount,
                PlatformFees = platformFees,
                CollectionRate = collectionRate,
                OrdersThisMonth = ordersThisMonth,
                MonthlyTrends = monthlyData,
                // Filter to show only registered users in breakdown tables
                RetailerBreakdowns = retailerBreakdowns.Where(r => r.Type == "Registered").OrderByDescending(r => r.PlatformFees).ToList(),
                WholesalerBreakdowns = wholesalerBreakdowns.Where(w => w.Type == "Registered").OrderByDescending(w => w.PlatformFees).ToList(),
                AvailableRetailers = availableRetailers,
                AvailableWholesalers = availableWholesalers
            };

            return View(viewModel);
        }

        private string ExtractExternalCustomerName(string notes)
        {
            if (notes.StartsWith("External Order for: "))
            {
                var lines = notes.Split('\n');
                return lines[0].Replace("External Order for: ", "").Trim();
            }
            return "Unknown External Customer";
        }

        private string ExtractPartnershipName(string notes)
        {
            if (notes.StartsWith("Partnership Order from: "))
            {
                var lines = notes.Split('\n');
                return lines[0].Replace("Partnership Order from: ", "").Trim();
            }
            return "Unknown Partnership";
        }

        // Simple users report
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            var userReports = new List<UserReportViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "Unknown";

                var orderCount = await _context.Orders
                    .Where(o => (role == "Retailer" && o.RetailerId == user.Id) ||
                               (role == "Wholesaler" && o.WholesalerId == user.Id))
                    .CountAsync();

                var totalSpent = role == "Retailer" ? 
                    await _context.Orders
                        .Where(o => o.RetailerId == user.Id)
                        .SumAsync(o => o.TotalAmount) : 0;

                var totalEarned = role == "Wholesaler" ?
                    await _context.Orders
                        .Where(o => o.WholesalerId == user.Id)
                        .SumAsync(o => o.TotalAmount) : 0;

                userReports.Add(new UserReportViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName ?? "",
                    BusinessName = user.BusinessName ?? "",
                    Email = user.Email ?? "",
                    Role = role,
                    RegistrationDate = DateTime.UtcNow.AddDays(-30), // Placeholder since CreatedAt doesn't exist
                    OrderCount = orderCount,
                    TotalSpent = totalSpent,
                    TotalEarned = totalEarned
                });
            }

            return View(userReports);
        }

        // Simple retailer purchases report
        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> RetailerPurchases(DateTime? startDate, DateTime? endDate, string? supplierId = null)
        {
            startDate ??= DateTime.UtcNow.AddMonths(-3).Date; // Start of day 3 months ago
            endDate ??= DateTime.UtcNow.Date.AddDays(1); // End of today (start of tomorrow)

            // Ensure we get the full day for user-selected end dates
            if (endDate.HasValue && endDate.Value.TimeOfDay == TimeSpan.Zero)
            {
                endDate = endDate.Value.AddDays(1); // Include entire end date
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Filter logic for both wholesalers and partners
            var orders = await _context.Orders
                .Include(o => o.Wholesaler)
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.WholesalerProduct)
                        .ThenInclude(wp => wp.Product)
                            .ThenInclude(p => p.Category)
                .Where(o => o.RetailerId == user.Id && 
                        o.OrderDate >= startDate && 
                           o.OrderDate < endDate &&  // Use < instead of <= for cleaner boundary logic
                           o.Status != OrderStatus.Cancelled)
                .ToListAsync();

            // Apply supplier filter after loading (to handle both wholesaler IDs and partner names)
            if (!string.IsNullOrEmpty(supplierId))
            {
                if (supplierId.StartsWith("PARTNER:"))
                {
                    // Partner filter: extract partner name and filter by partnership orders only
                    var partnerName = supplierId.Substring("PARTNER:".Length);
                    orders = orders.Where(o => o.WholesalerId == null && 
                                             o.Notes != null && 
                                             o.Notes.StartsWith($"Partnership Order from: {partnerName}"))
                .ToList();
                    }
                    else
                    {
                    // Wholesaler filter: filter by WholesalerId
                    orders = orders.Where(o => o.WholesalerId == supplierId).ToList();
                }
            }

            orders = orders.OrderByDescending(o => o.OrderDate).ToList();

            var orderIds = orders.Select(o => o.Id).ToList();
            var transactions = await _context.Transactions
                .Include(t => t.Payments)
                .Where(t => orderIds.Contains(t.OrderId))
                .ToListAsync();
                
            var transactionLookup = transactions.GroupBy(t => t.OrderId).ToDictionary(g => g.Key, g => g.First());

            // Get all suppliers (both wholesalers and partners) this retailer has ever ordered from
            var allRetailerOrders = await _context.Orders
                .Where(o => o.RetailerId == user.Id)
                .Include(o => o.Wholesaler)
                    .ToListAsync();
                    
            var suppliers = new List<SupplierOption>();

            // Add wholesalers
            var wholesalerIds = allRetailerOrders
                .Where(o => o.WholesalerId != null)
                .Select(o => o.WholesalerId!)
                .Distinct()
                .ToList();

            var wholesalers = await _context.Users
                .Where(u => wholesalerIds.Contains(u.Id))
                .ToListAsync();

            suppliers.AddRange(wholesalers.Select(w => new SupplierOption
            {
                Id = w.Id,
                Name = w.BusinessName ?? w.UserName ?? "Unknown",
                Type = "Wholesaler"
            }));

            // Add partners (extract ONLY from partnership orders - external orders have RetailerId=null so never appear in retailer reports)
            var partnershipOrders = allRetailerOrders
                .Where(o => o.WholesalerId == null && 
                           o.Notes != null &&
                           o.Notes.StartsWith("Partnership Order from: "))
                .ToList();

            var partnerNames = partnershipOrders
                .Select(o => o.Notes!.Substring("Partnership Order from: ".Length).Split('\n')[0])
                .Distinct()
                .ToList();

            suppliers.AddRange(partnerNames.Select(p => new SupplierOption
            {
                Id = $"PARTNER:{p}",
                Name = p,
                Type = "Partner"
            }));

            // Sort suppliers by type then by name
            suppliers = suppliers.OrderBy(s => s.Type).ThenBy(s => s.Name).ToList();

            // Calculate spending by category (include both wholesaler and partnership orders)
            var wholesalerCategorySpending = orders
                .SelectMany(o => o.Items ?? new List<OrderItem>())
                .Where(i => i.WholesalerProduct?.Product?.Category != null)
                .GroupBy(i => new { 
                    CategoryId = i.WholesalerProduct.Product.Category.Id,
                    CategoryName = i.WholesalerProduct.Product.Category.Name
                })
                .Select(g => new {
                    CategoryName = g.Key.CategoryName,
                    TotalSpent = g.Sum(i => i.Subtotal),
                    OrderCount = g.Select(i => i.OrderId).Distinct().Count(),
                    ProductCount = g.Select(i => i.WholesalerProduct.ProductId).Distinct().Count()
                });

            // Get partnership order category spending
            var partnershipOrderIds = orders.Where(o => o.WholesalerId == null).Select(o => o.Id).ToList();
            var partnershipOrderItems = await _context.OrderItems
                .Where(oi => partnershipOrderIds.Contains(oi.OrderId) && 
                            oi.PartnerProductId != null)
                .ToListAsync();

            // Get partner products with categories for these order items
            var partnerProductIds = partnershipOrderItems.Select(oi => oi.PartnerProductId!.Value).Distinct().ToList();
            var partnerProducts = await _context.RetailerPartnerProducts
                .Include(rpp => rpp.Category)
                .Where(rpp => partnerProductIds.Contains(rpp.Id))
                .ToListAsync();

            var partnershipCategorySpending = partnershipOrderItems
                .Join(partnerProducts, 
                      oi => oi.PartnerProductId, 
                      rpp => rpp.Id, 
                      (oi, rpp) => new { OrderItem = oi, PartnerProduct = rpp })
                .Where(x => x.PartnerProduct.Category != null)
                .GroupBy(x => new {
                    CategoryId = x.PartnerProduct.Category!.Id,
                    CategoryName = x.PartnerProduct.Category.Name
                })
                .Select(g => new {
                    CategoryName = g.Key.CategoryName,
                    TotalSpent = g.Sum(x => x.OrderItem.Subtotal),
                    OrderCount = g.Select(x => x.OrderItem.OrderId).Distinct().Count(),
                    ProductCount = g.Select(x => x.OrderItem.PartnerProductId).Distinct().Count()
                })
                .ToList();

            // Combine and aggregate both types of spending
            var allCategorySpending = wholesalerCategorySpending.ToList()
                .Concat(partnershipCategorySpending)
                .GroupBy(c => c.CategoryName)
                .Select(g => new RetailerCategorySpending
                {
                    CategoryName = g.Key,
                    TotalSpent = g.Sum(c => c.TotalSpent),
                    OrderCount = g.Sum(c => c.OrderCount),
                    ProductCount = g.Sum(c => c.ProductCount)
                })
                .OrderByDescending(c => c.TotalSpent)
                .Take(5)
                .ToList();

            var viewModel = new RetailerPurchasesViewModel
            {
                StartDate = startDate.Value,
                EndDate = endDate.Value,
                SupplierId = supplierId,
                Orders = orders,
                TransactionLookup = transactionLookup,
                Suppliers = suppliers,
                TotalSpent = orders.Sum(o => o.TotalAmount),
                TotalPaid = orders.Where(o => transactionLookup.ContainsKey(o.Id))
                                 .Sum(o => transactionLookup[o.Id].AmountPaid),
                SpendingByCategory = allCategorySpending
            };

            return View(viewModel);
        }

        // System maintenance function
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePaymentStatuses()
        {
            var transactions = await _context.Transactions
                .Include(t => t.Payments)
                .ToListAsync();

            var updatedCount = 0;
            foreach (var transaction in transactions)
            {
                var actualAmountPaid = transaction.Payments?.Sum(p => p.Amount) ?? 0;
                if (transaction.AmountPaid != actualAmountPaid)
                {
                    transaction.AmountPaid = actualAmountPaid;
                    updatedCount++;
                }

                var newStatus = actualAmountPaid >= transaction.Amount ? TransactionStatus.Completed :
                               actualAmountPaid > 0 ? TransactionStatus.PartiallyPaid : TransactionStatus.Pending;

                if (transaction.Status != newStatus)
                {
                    transaction.Status = newStatus;
                    updatedCount++;
                }
            }

            if (updatedCount > 0)
            {
            await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"Updated {updatedCount} payment records.";
            return RedirectToAction("Index");
        }
    }
} 