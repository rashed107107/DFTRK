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
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrdersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string status)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Parse status filter if provided
            OrderStatus? statusFilter = null;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var parsedStatus))
            {
                statusFilter = parsedStatus;
                ViewBag.CurrentStatus = statusFilter;
            }

            var orders = new List<Order>();
            var isRetailer = User.IsInRole("Retailer");
            var isWholesaler = User.IsInRole("Wholesaler");
            var isAdmin = User.IsInRole("Admin");

            if (isRetailer)
            {
                // Build the query step by step - only internal orders (with RetailerId)
                var query = _context.Orders.Where(o => o.RetailerId == user.Id);
                
                // Apply status filter if provided
                if (statusFilter.HasValue)
                {
                    query = query.Where(o => o.Status == statusFilter.Value);
                }
                
                // Apply includes and ordering after filtering
                orders = await query
                    .Include(o => o.Wholesaler)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
            }
            else if (isWholesaler)
            {
                // Build the query step by step - only internal orders (with RetailerId)
                var query = _context.Orders.Where(o => o.WholesalerId == user.Id && o.RetailerId != null);
                
                // Apply status filter if provided
                if (statusFilter.HasValue)
                {
                    query = query.Where(o => o.Status == statusFilter.Value);
                }
                
                // Apply includes and ordering after filtering
                orders = await query
                    .Include(o => o.Retailer)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
            }
            else if (isAdmin)
            {
                // Build the query step by step - only internal orders (with RetailerId)
                var query = _context.Orders.Where(o => o.RetailerId != null);
                
                // Apply status filter if provided
                if (statusFilter.HasValue)
                {
                    query = query.Where(o => o.Status == statusFilter.Value);
                }
                
                // Apply includes and ordering after filtering
                orders = await query
                    .Include(o => o.Retailer)
                    .Include(o => o.Wholesaler)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
            }

            return View(orders);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Retailer)
                .Include(o => o.Wholesaler)
                .Include(o => o.Items)
                .ThenInclude(i => i.WholesalerProduct)
                .ThenInclude(wp => wp.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // Check authorization - users can only view their own orders
            if (User.IsInRole("Wholesaler") && order.WholesalerId != user.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            else if (User.IsInRole("Retailer") && order.RetailerId != user.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(order);
        }

        [HttpGet]
        [Authorize(Roles = "Wholesaler")]
        public async Task<IActionResult> Process(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(oi => oi.WholesalerProduct)
                .FirstOrDefaultAsync(o => o.Id == id && o.WholesalerId == user.Id && o.Status == OrderStatus.Pending);

            if (order == null)
            {
                return NotFound();
            }

            // Validate stock availability before processing
            var stockIssues = new List<string>();
            foreach (var orderItem in order.Items)
            {
                if (orderItem.WholesalerProduct != null)
                {
                    if (orderItem.WholesalerProduct.StockQuantity < orderItem.Quantity)
                    {
                        stockIssues.Add($"{orderItem.WholesalerProduct.Product?.Name}: Need {orderItem.Quantity}, but only {orderItem.WholesalerProduct.StockQuantity} available");
                    }
                }
            }

            if (stockIssues.Any())
            {
                TempData["Error"] = "Cannot process order due to insufficient stock: " + string.Join("; ", stockIssues);
                return RedirectToAction(nameof(Details), new { id = order.Id });
            }

            // Update order status
            order.Status = OrderStatus.Processing;
            _context.Update(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Order processed successfully and is ready for shipping.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        [HttpGet]
        [Authorize(Roles = "Wholesaler")]
        public async Task<IActionResult> Ship(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(oi => oi.WholesalerProduct)
                .FirstOrDefaultAsync(o => o.Id == id && o.WholesalerId == user.Id && o.Status == OrderStatus.Processing);

            if (order == null)
            {
                return NotFound();
            }

            // Validate stock availability before shipping
            var stockIssues = new List<string>();
            foreach (var orderItem in order.Items)
            {
                if (orderItem.WholesalerProduct != null)
                {
                    if (orderItem.WholesalerProduct.StockQuantity < orderItem.Quantity)
                    {
                        stockIssues.Add($"{orderItem.WholesalerProduct.Product?.Name}: Need {orderItem.Quantity}, but only {orderItem.WholesalerProduct.StockQuantity} available");
                    }
                }
            }

            if (stockIssues.Any())
            {
                TempData["Error"] = "Cannot ship order due to insufficient stock: " + string.Join("; ", stockIssues);
                return RedirectToAction(nameof(Details), new { id = order.Id });
            }

            // Update order status
            order.Status = OrderStatus.Shipped;
            _context.Update(order);

            // Deduct stock quantities from wholesaler inventory
            foreach (var orderItem in order.Items)
            {
                if (orderItem.WholesalerProduct != null)
                {
                    // Decrease the stock quantity
                    orderItem.WholesalerProduct.StockQuantity -= orderItem.Quantity;
                    
                    _context.Update(orderItem.WholesalerProduct);
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Order shipped successfully and inventory has been updated.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        [HttpGet]
        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> ConfirmDelivery(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.WholesalerProduct)
                .FirstOrDefaultAsync(o => o.Id == id && o.RetailerId == user.Id && o.Status == OrderStatus.Shipped);

            if (order == null)
            {
                return NotFound();
            }

            // Update order status
            order.Status = OrderStatus.Delivered;
            _context.Update(order);

            // Check if this is a partnership order
            bool isPartnershipOrder = order.WholesalerId == null;
            
            // Add products to retailer inventory
            if (!isPartnershipOrder)
            {
                // Regular wholesaler orders
                foreach (var orderItem in order.Items)
                {
                    if (orderItem.WholesalerProductId.HasValue)
                    {
                        // Check if product already exists in retailer inventory
                        var existingProduct = await _context.RetailerProducts
                            .FirstOrDefaultAsync(rp => rp.RetailerId == user.Id && 
                                                 rp.WholesalerProductId == orderItem.WholesalerProductId.Value);

                        if (existingProduct != null)
                        {
                            // Update existing product stock and potentially update purchase price
                            existingProduct.StockQuantity += orderItem.Quantity;
                            
                            // Optionally update the purchase price with latest price
                            existingProduct.PurchasePrice = orderItem.UnitPrice;
                            
                            _context.Update(existingProduct);
                        }
                        else
                        {
                            // Add new product to retailer inventory
                            var retailerProduct = new RetailerProduct
                            {
                                RetailerId = user.Id,
                                WholesalerProductId = orderItem.WholesalerProductId.Value,
                                PurchasePrice = orderItem.UnitPrice,
                                SellingPrice = orderItem.UnitPrice * 1.3m, // Default 30% markup
                                StockQuantity = orderItem.Quantity,
                                Notes = "Added from order #" + id
                            };
                            
                            _context.RetailerProducts.Add(retailerProduct);
                        }
                    }
                }
            }
            else
            {
                // Partnership orders - add as partnership products to retailer inventory
                foreach (var orderItem in order.Items)
                {
                    if (orderItem.PartnerProductId.HasValue && !string.IsNullOrEmpty(orderItem.ProductName))
                    {
                        // Check if this partnership product already exists in retailer inventory
                        var existingProduct = await _context.RetailerProducts
                            .FirstOrDefaultAsync(rp => rp.RetailerId == user.Id && 
                                                 rp.Notes != null &&
                                                 rp.Notes.Contains($"Partnership product: {orderItem.ProductName}"));

                        if (existingProduct != null)
                        {
                            // Update existing partnership product stock
                            existingProduct.StockQuantity += orderItem.Quantity;
                            existingProduct.PurchasePrice = orderItem.UnitPrice;
                            existingProduct.Notes = $"Partnership product: {orderItem.ProductName} - Last updated from order #{id}";
                            
                            _context.Update(existingProduct);
                        }
                        else
                        {
                            // Add new partnership product to retailer inventory
                            var retailerProduct = new RetailerProduct
                            {
                                RetailerId = user.Id,
                                WholesalerProductId = null, // No wholesaler product for partnerships
                                PurchasePrice = orderItem.UnitPrice,
                                SellingPrice = orderItem.UnitPrice * 1.3m, // Default 30% markup
                                StockQuantity = orderItem.Quantity,
                                Notes = $"Partnership product: {orderItem.ProductName} - Added from order #{id}"
                            };
                            
                            _context.RetailerProducts.Add(retailerProduct);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Set appropriate success message
            if (isPartnershipOrder)
            {
                TempData["Success"] = "Partnership order marked as delivered successfully. Partnership products have been added to your inventory.";
            }
            else
            {
                TempData["Success"] = "Order marked as delivered and items have been added to your inventory. Please review your inventory to confirm the quantities.";
            }
            
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        [HttpGet]
        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> Complete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.RetailerId == user.Id && o.Status == OrderStatus.Delivered);

            if (order == null)
            {
                return NotFound();
            }

            // Update order status
            order.Status = OrderStatus.Completed;
            _context.Update(order);

            // Update transaction status if exists and payment is complete
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.OrderId == order.Id);
                
            if (transaction != null)
            {
                // Only mark as completed if fully paid
                if (transaction.AmountPaid >= transaction.Amount)
                {
                    transaction.Status = TransactionStatus.Completed;
                }
                else if (transaction.AmountPaid > 0)
                {
                    transaction.Status = TransactionStatus.PartiallyPaid;
                }
                // Otherwise, leave as Pending
                
                _context.Update(transaction);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Order completed successfully!";

            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        [HttpGet]
        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> Cancel(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(oi => oi.WholesalerProduct)
                .FirstOrDefaultAsync(o => o.Id == id && o.RetailerId == user.Id && 
                    (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Processing));

            if (order == null)
            {
                return NotFound();
            }

            // Check if this is a partnership order (WholesalerId is null)
            bool isPartnershipOrder = order.WholesalerId == null;

            // If order was already processed but not shipped, we need to restore stock (only for regular orders)
            bool needsStockRestoration = order.Status == OrderStatus.Processing && !isPartnershipOrder;

            // Update order status
            order.Status = OrderStatus.Cancelled;
            _context.Update(order);

            // Restore stock if the order was already processed (only for regular orders with wholesaler products)
            if (needsStockRestoration)
            {
                foreach (var orderItem in order.Items)
                {
                    if (orderItem.WholesalerProduct != null)
                    {
                        // Restore the stock quantity
                        orderItem.WholesalerProduct.StockQuantity += orderItem.Quantity;
                        _context.Update(orderItem.WholesalerProduct);
                    }
                }
            }

            // Update transaction status if exists
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.OrderId == order.Id);
                
            if (transaction != null)
            {
                transaction.Status = TransactionStatus.Refunded;
                _context.Update(transaction);
            }

            await _context.SaveChangesAsync();

            // Set appropriate success message
            if (isPartnershipOrder)
            {
                TempData["Success"] = "Partnership order cancelled successfully.";
            }
            else
            {
                TempData["Success"] = needsStockRestoration ? 
                    "Order cancelled successfully and stock has been restored." : 
                    "Order cancelled successfully.";
            }
            
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        [HttpGet]
        [Authorize(Roles = "Wholesaler")]
        public async Task<IActionResult> CancelByWholesaler(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(oi => oi.WholesalerProduct)
                .FirstOrDefaultAsync(o => o.Id == id && o.WholesalerId == user.Id && 
                    (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Processing));

            if (order == null)
            {
                return NotFound();
            }

            // If order was already processed/shipped, we might need to restore stock
            bool needsStockRestoration = order.Status == OrderStatus.Processing;

            // Update order status
            order.Status = OrderStatus.Cancelled;
            _context.Update(order);

            // Restore stock if the order was already processed but not shipped
            if (needsStockRestoration)
            {
                foreach (var orderItem in order.Items)
                {
                    if (orderItem.WholesalerProduct != null)
                    {
                        // Restore the stock quantity
                        orderItem.WholesalerProduct.StockQuantity += orderItem.Quantity;
                        _context.Update(orderItem.WholesalerProduct);
                    }
                }
            }

            // Update transaction status if exists
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.OrderId == order.Id);
                
            if (transaction != null)
            {
                transaction.Status = TransactionStatus.Refunded;
                _context.Update(transaction);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = needsStockRestoration ? 
                "Order cancelled successfully and stock has been restored." : 
                "Order cancelled successfully.";

            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        // GET: Orders/Pay/5
        [HttpGet]
        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> Pay(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.RetailerId == user.Id);

            if (order == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.OrderId == order.Id);

            if (transaction == null)
            {
                TempData["Error"] = "No transaction found for this order.";
                return RedirectToAction(nameof(Details), new { id = order.Id });
            }

            // Redirect directly to the MakePayment action in PaymentsController
            return RedirectToAction("MakePayment", "Payments", new { id = transaction.Id });
        }

        #region Partnership Order Management

        [HttpGet]
        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> CompletePartnershipOrder(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id && 
                                         o.RetailerId == user.Id && 
                                         o.WholesalerId == null && // Partnership order
                                         o.Status != OrderStatus.Completed &&
                                         o.Status != OrderStatus.Cancelled);

            if (order == null)
            {
                return NotFound();
            }

            // Update order status to completed
            order.Status = OrderStatus.Completed;
            _context.Update(order);

            // Add partnership products to retailer inventory
            int addedProductsCount = 0;
            int updatedProductsCount = 0;
            int skippedItemsCount = 0;
            var processedItems = new List<string>();
            
            if (order.Items == null || !order.Items.Any())
            {
                TempData["Warning"] = "Order has no items to process.";
                return RedirectToAction(nameof(Details), new { id = order.Id });
            }
            
            // Debug: Log all order item data
            processedItems.Add($"Order #{id} has {order.Items.Count()} items total");
            
            foreach (var orderItem in order.Items)
            {
                // Debug: Log each item's data
                processedItems.Add($"Item {orderItem.Id}: PartnerProductId={orderItem.PartnerProductId?.ToString() ?? "NULL"}, ProductName='{orderItem.ProductName ?? "NULL"}', WholesalerProductId={orderItem.WholesalerProductId?.ToString() ?? "NULL"}, Quantity={orderItem.Quantity}");
                
                if (orderItem.PartnerProductId.HasValue && !string.IsNullOrEmpty(orderItem.ProductName))
                {
                    // Check if this partnership product already exists in retailer inventory
                    var existingProduct = await _context.RetailerProducts
                        .FirstOrDefaultAsync(rp => rp.RetailerId == user.Id && 
                                             rp.Notes != null &&
                                             rp.Notes.Contains($"Partnership product: {orderItem.ProductName}"));

                    if (existingProduct != null)
                    {
                        // Update existing partnership product stock
                        existingProduct.StockQuantity += orderItem.Quantity;
                        existingProduct.PurchasePrice = orderItem.UnitPrice;
                        existingProduct.Notes = $"Partnership product: {orderItem.ProductName} - Last updated from order #{id}";
                        
                        _context.Update(existingProduct);
                        updatedProductsCount++;
                        processedItems.Add($"Updated {orderItem.ProductName} (+{orderItem.Quantity})");
                    }
                    else
                    {
                        // Add new partnership product to retailer inventory
                        var retailerProduct = new RetailerProduct
                        {
                            RetailerId = user.Id,
                            WholesalerProductId = null, // No wholesaler product for partnerships
                            PurchasePrice = orderItem.UnitPrice,
                            SellingPrice = orderItem.UnitPrice * 1.3m, // Default 30% markup
                            StockQuantity = orderItem.Quantity,
                            Notes = $"Partnership product: {orderItem.ProductName} - Added from order #{id}"
                        };
                        
                        _context.RetailerProducts.Add(retailerProduct);
                        addedProductsCount++;
                        processedItems.Add($"Added {orderItem.ProductName} ({orderItem.Quantity} units)");
                    }
                }
                else
                {
                    skippedItemsCount++;
                    processedItems.Add($"Skipped item - PartnerProductId: {orderItem.PartnerProductId?.ToString() ?? "null"}, ProductName: '{orderItem.ProductName ?? "null"}'");
                }
            }

            // Update transaction status if exists
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.OrderId == order.Id);
                
            if (transaction != null)
            {
                // Only mark as completed if fully paid
                if (transaction.AmountPaid >= transaction.Amount)
                {
                    transaction.Status = TransactionStatus.Completed;
                }
                else if (transaction.AmountPaid > 0)
                {
                    transaction.Status = TransactionStatus.PartiallyPaid;
                }
                // Otherwise, leave as Pending
                
                _context.Update(transaction);
            }

            await _context.SaveChangesAsync();
            
            // Create detailed success message
            string message = $"Partnership order completed successfully! ";
            if (addedProductsCount > 0 || updatedProductsCount > 0)
            {
                message += $"{addedProductsCount} new products added, {updatedProductsCount} existing products updated in your inventory.";
            }
            else
            {
                message += "No products were added to inventory.";
            }
            
            if (skippedItemsCount > 0)
            {
                message += $" {skippedItemsCount} items were skipped.";
            }
            
            // For debugging, show detailed processing info
            TempData["Success"] = message;
            TempData["ProcessingDetails"] = string.Join("<br/>", processedItems);

            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        #endregion
    }
} 