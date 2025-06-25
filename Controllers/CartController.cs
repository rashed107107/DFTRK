using DFTRK.Data;
using DFTRK.Models;
using DFTRK.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DFTRK.Controllers
{
    [Authorize(Roles = "Retailer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Cart
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.WholesalerProduct)
                .ThenInclude(wp => wp.Product)
                .Include(c => c.Items)
                .ThenInclude(i => i.WholesalerProduct)
                .ThenInclude(wp => wp.Wholesaler)
                .FirstOrDefaultAsync(c => c.RetailerId == user.Id);

            if (cart == null || cart.Items == null || !cart.Items.Any())
            {
                // Empty cart
                ViewBag.CartItemCount = 0;
                return View(new CartViewModel
                {
                    Items = new List<CartItemViewModel>(),
                    TotalAmount = 0
                });
            }

            var viewModel = new CartViewModel
            {
                Items = cart.Items.Select(item => new CartItemViewModel
                {
                    Id = item.Id,
                    WholesalerProductId = item.WholesalerProductId,
                    ProductName = item.WholesalerProduct.Product.Name,
                    ProductImageUrl = item.WholesalerProduct.Product.ImageUrl,
                    WholesalerId = item.WholesalerProduct.WholesalerId,
                    WholesalerName = item.WholesalerProduct.Wholesaler.BusinessName,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price,
                    Subtotal = item.Price * item.Quantity
                }).ToList(),
                TotalAmount = cart.Items.Sum(i => i.Price * i.Quantity)
            };

            ViewBag.CartItemCount = viewModel.Items.Count;
            return View(viewModel);
        }

        // POST: Cart/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity, string? action)
        {
            if (quantity < 1)
            {
                TempData["Error"] = "Quantity must be at least 1";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .Include(ci => ci.WholesalerProduct)
                .ThenInclude(wp => wp.Product)
                .FirstOrDefaultAsync(ci => ci.Id == id && ci.Cart.RetailerId == user.Id);

            if (cartItem == null)
            {
                return NotFound();
            }

            int newQuantity = cartItem.Quantity;

            if (action == "increase")
            {
                newQuantity = cartItem.Quantity + 1;
            }
            else if (action == "decrease")
            {
                newQuantity = Math.Max(1, cartItem.Quantity - 1);
            }
            else
            {
                // Direct quantity update (when user types in the input field)
                newQuantity = quantity;
            }

            // Check if we have enough stock
            if (newQuantity > cartItem.WholesalerProduct.StockQuantity)
            {
                newQuantity = cartItem.WholesalerProduct.StockQuantity;
                TempData["Warning"] = $"Quantity adjusted to {newQuantity} to match available stock for {cartItem.WholesalerProduct.Product?.Name}.";
            }

            // Ensure quantity is at least 1
            newQuantity = Math.Max(1, newQuantity);

            cartItem.Quantity = newQuantity;
            _context.Update(cartItem);
            await _context.SaveChangesAsync();

            if (action == null)
            {
                TempData["Success"] = "Quantity updated successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/Remove/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == id && ci.Cart.RetailerId == user.Id);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Cart/Checkout
        public async Task<IActionResult> Checkout(string wholesalerId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.WholesalerProduct)
                .ThenInclude(wp => wp.Product)
                .Include(c => c.Items)
                .ThenInclude(i => i.WholesalerProduct)
                .ThenInclude(wp => wp.Wholesaler)
                .FirstOrDefaultAsync(c => c.RetailerId == user.Id);

            if (cart == null || cart.Items == null || !cart.Items.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction(nameof(Index));
            }

            // Get items for this wholesaler only
            var wholesalerItems = cart.Items
                .Where(i => i.WholesalerProduct?.WholesalerId == wholesalerId)
                .ToList();

            if (!wholesalerItems.Any())
            {
                TempData["Error"] = "No items found for this wholesaler.";
                return RedirectToAction(nameof(Index));
            }

            // Get wholesaler info
            var wholesaler = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == wholesalerId);

            if (wholesaler == null)
            {
                TempData["Error"] = "Wholesaler not found.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new CheckoutViewModel
            {
                Items = wholesalerItems.Select(item => new CartItemViewModel
                {
                    Id = item.Id,
                    WholesalerProductId = item.WholesalerProductId,
                    ProductName = item.WholesalerProduct.Product.Name,
                    ProductImageUrl = item.WholesalerProduct.Product.ImageUrl,
                    WholesalerId = item.WholesalerProduct.WholesalerId,
                    WholesalerName = item.WholesalerProduct.Wholesaler.BusinessName,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price,
                    Subtotal = item.Price * item.Quantity
                }).ToList(),
                TotalAmount = wholesalerItems.Sum(i => i.Price * i.Quantity),
                // Pre-fill with user data
                BusinessName = user.BusinessName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                City = user.City,
                State = user.State,
                PostalCode = user.PostalCode,
                Country = user.Country
            };

            ViewBag.WholesalerId = wholesalerId;
            ViewBag.WholesalerName = wholesaler.BusinessName;

            return View(viewModel);
        }

        // POST: Cart/ProcessOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessOrder(string wholesalerId, CheckoutViewModel model, string paymentMethod)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.WholesalerProduct)
                .FirstOrDefaultAsync(c => c.RetailerId == user.Id);

            if (cart == null || cart.Items == null || !cart.Items.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction(nameof(Index));
            }

            // Get items for this wholesaler only
            var wholesalerItems = cart.Items
                .Where(i => i.WholesalerProduct?.WholesalerId == wholesalerId)
                .ToList();

            if (!wholesalerItems.Any())
            {
                TempData["Error"] = "No items found for this wholesaler.";
                return RedirectToAction(nameof(Index));
            }

            // Create order
            var order = new Order
            {
                RetailerId = user.Id,
                WholesalerId = wholesalerId,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                TotalAmount = wholesalerItems.Sum(i => i.Price * i.Quantity),
                Notes = model.Notes,
                Items = wholesalerItems.Select(item => new OrderItem
                {
                    WholesalerProductId = item.WholesalerProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price,
                    Subtotal = item.Price * item.Quantity
                }).ToList()
            };

            _context.Orders.Add(order);

            // Create transaction record
            var transaction = new Transaction
            {
                RetailerId = user.Id,
                WholesalerId = wholesalerId,
                Amount = order.TotalAmount,
                AmountPaid = 0, // No payment made yet
                TransactionDate = DateTime.UtcNow,
                Status = TransactionStatus.Pending,
                Order = order,
                PaymentMethod = paymentMethod ?? "Credit Card", // Use selected payment method
                TransactionReference = $"ORDER-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8)}"
            };

            _context.Transactions.Add(transaction);

            // Remove items from cart
            _context.CartItems.RemoveRange(wholesalerItems);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your order has been placed successfully! Proceed to payment to complete your order.";
            return RedirectToAction("Details", "Payments", new { id = transaction.Id });
        }

        // POST: Cart/Clear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.RetailerId == user.Id);

            if (cart != null && cart.Items != null)
            {
                _context.CartItems.RemoveRange(cart.Items);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
} 