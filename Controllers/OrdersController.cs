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
                // Build the query step by step
                var query = _context.Orders.Where(o => o.RetailerId == user.Id);
                
                // Apply status filter if provided
                if (statusFilter.HasValue)
                {
                    query = query.Where(o => o.Status == statusFilter.Value);
                }
                
                // Apply includes and ordering after filtering
                orders = await query
                    .Include(o => o.Wholesaler)
                    .Include(o => o.Transaction)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
            }
            else if (isWholesaler)
            {
                // Build the query step by step
                var query = _context.Orders.Where(o => o.WholesalerId == user.Id);
                
                // Apply status filter if provided
                if (statusFilter.HasValue)
                {
                    query = query.Where(o => o.Status == statusFilter.Value);
                }
                
                // Apply includes and ordering after filtering
                orders = await query
                    .Include(o => o.Retailer)
                    .Include(o => o.Transaction)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
            }
            else if (isAdmin)
            {
                // Build the query step by step
                var query = _context.Orders.AsQueryable();
                
                // Apply status filter if provided
                if (statusFilter.HasValue)
                {
                    query = query.Where(o => o.Status == statusFilter.Value);
                }
                
                // Apply includes and ordering after filtering
                orders = await query
                    .Include(o => o.Retailer)
                    .Include(o => o.Wholesaler)
                    .Include(o => o.Transaction)
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
                .Include(o => o.Transaction)
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
                .FirstOrDefaultAsync(o => o.Id == id && o.WholesalerId == user.Id && o.Status == OrderStatus.Pending);

            if (order == null)
            {
                return NotFound();
            }

            // Update order status
            order.Status = OrderStatus.Processing;
            _context.Update(order);
            await _context.SaveChangesAsync();

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
                .FirstOrDefaultAsync(o => o.Id == id && o.WholesalerId == user.Id && o.Status == OrderStatus.Processing);

            if (order == null)
            {
                return NotFound();
            }

            // Update order status
            order.Status = OrderStatus.Shipped;
            _context.Update(order);
            await _context.SaveChangesAsync();

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

            // Add products to retailer inventory
            foreach (var orderItem in order.Items)
            {
                // Check if product already exists in retailer inventory
                var existingProduct = await _context.RetailerProducts
                    .FirstOrDefaultAsync(rp => rp.RetailerId == user.Id && 
                                         rp.WholesalerProductId == orderItem.WholesalerProductId);

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
                        WholesalerProductId = orderItem.WholesalerProductId,
                        PurchasePrice = orderItem.UnitPrice,
                        SellingPrice = orderItem.UnitPrice * 1.3m, // Default 30% markup
                        StockQuantity = orderItem.Quantity,
                        Notes = "Added from order #" + id
                    };
                    
                    _context.RetailerProducts.Add(retailerProduct);
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Order marked as delivered and items have been added to your inventory. Please review your inventory to confirm the quantities.";
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
                .Include(o => o.Transaction)
                .FirstOrDefaultAsync(o => o.Id == id && o.RetailerId == user.Id && o.Status == OrderStatus.Delivered);

            if (order == null)
            {
                return NotFound();
            }

            // Update order status
            order.Status = OrderStatus.Completed;
            _context.Update(order);

            // Update transaction status if exists and payment is complete
            if (order.Transaction != null)
            {
                // Only mark as completed if fully paid
                if (order.Transaction.AmountPaid >= order.Transaction.Amount)
                {
                    order.Transaction.Status = TransactionStatus.Completed;
                }
                else if (order.Transaction.AmountPaid > 0)
                {
                    order.Transaction.Status = TransactionStatus.PartiallyPaid;
                }
                // Otherwise, leave as Pending
                
                _context.Update(order.Transaction);
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
                .Include(o => o.Transaction)
                .FirstOrDefaultAsync(o => o.Id == id && o.RetailerId == user.Id && o.Status == OrderStatus.Pending);

            if (order == null)
            {
                return NotFound();
            }

            // Update order status
            order.Status = OrderStatus.Cancelled;
            _context.Update(order);

            // Update transaction status if exists
            if (order.Transaction != null)
            {
                order.Transaction.Status = TransactionStatus.Refunded;
                _context.Update(order.Transaction);
            }

            await _context.SaveChangesAsync();

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
                .Include(o => o.Transaction)
                .FirstOrDefaultAsync(o => o.Id == id && o.RetailerId == user.Id);

            if (order == null)
            {
                return NotFound();
            }

            if (order.Transaction == null)
            {
                TempData["Error"] = "No transaction found for this order.";
                return RedirectToAction(nameof(Details), new { id = order.Id });
            }

            // Redirect directly to the MakePayment action in PaymentsController
            return RedirectToAction("MakePayment", "Payments", new { id = order.Transaction.Id });
        }
    }
} 