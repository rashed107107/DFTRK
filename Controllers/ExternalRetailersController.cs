using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DFTRK.Data;
using DFTRK.Models;
using DFTRK.ViewModels;

namespace DFTRK.Controllers
{
    [Authorize(Roles = "Wholesaler")]
    public class ExternalRetailersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExternalRetailersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: ExternalRetailers - List all external orders for this wholesaler
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Get all external orders for this wholesaler
            var externalOrders = await _context.Orders
                .Where(o => o.WholesalerId == user.Id && o.RetailerId == null)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Get transactions for these orders
            var orderIds = externalOrders.Select(o => o.Id).ToList();
            var transactions = await _context.Transactions
                .Include(t => t.Payments)
                .Where(t => orderIds.Contains(t.OrderId))
                .ToListAsync();

            var viewModels = new List<ExternalOrderViewModel>();

            foreach (var order in externalOrders)
            {
                var transaction = transactions.FirstOrDefault(t => t.OrderId == order.Id);
                var amountPaid = transaction?.Payments?.Sum(p => p.Amount) ?? 0;

                // Extract customer name from notes
                string customerName = "Unknown Customer";
                if (!string.IsNullOrEmpty(order.Notes) && order.Notes.StartsWith("External Order for: "))
                {
                    customerName = order.Notes.Split('\n')[0].Replace("External Order for: ", "").Trim();
                }

                viewModels.Add(new ExternalOrderViewModel
                {
                    OrderId = order.Id,
                    CustomerName = customerName,
                    OrderDate = order.OrderDate,
                    TotalAmount = order.TotalAmount,
                    AmountPaid = amountPaid,
                    Outstanding = order.TotalAmount - amountPaid,
                    Status = order.Status.ToString(),
                    PaymentStatus = amountPaid >= order.TotalAmount ? "Paid" : 
                                   amountPaid > 0 ? "Partial" : "Unpaid"
                });
            }

            return View(viewModels);
        }

        // GET: ExternalRetailers/Create - Create new external order
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var products = await _context.WholesalerProducts
                .Include(wp => wp.Product)
                .Where(wp => wp.WholesalerId == user.Id && wp.StockQuantity > 0)
                .ToListAsync();

            var viewModel = new CreateExternalOrderViewModel
            {
                AvailableProducts = products.Select(wp => new ProductSelectionItem
                {
                    ProductId = wp.Id,
                    ProductName = wp.Product.Name,
                    Price = wp.Price,
                    AvailableStock = wp.StockQuantity
                }).ToList()
            };

            return View(viewModel);
        }

        // POST: ExternalRetailers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateExternalOrderViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (ModelState.IsValid && model.SelectedProducts?.Any() == true)
            {
                // Create the order
                var order = new Order
                {
                    WholesalerId = user.Id,
                    RetailerId = null, // External order
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.Completed, // External orders are completed immediately
                    Notes = $"External Order for: {model.CustomerName}\nCustomer Contact: {model.CustomerContact}",
                    Items = new List<OrderItem>()
                };

                decimal totalAmount = 0;

                // Add items and update inventory
                foreach (var selectedProduct in model.SelectedProducts.Where(p => p.Quantity > 0))
                {
                    var wholesalerProduct = await _context.WholesalerProducts
                        .FirstOrDefaultAsync(wp => wp.Id == selectedProduct.ProductId && wp.WholesalerId == user.Id);

                    if (wholesalerProduct != null && wholesalerProduct.StockQuantity >= selectedProduct.Quantity)
                    {
                        var orderItem = new OrderItem
                        {
                            WholesalerProductId = wholesalerProduct.Id,
                            Quantity = selectedProduct.Quantity,
                            UnitPrice = wholesalerProduct.Price,
                            Subtotal = wholesalerProduct.Price * selectedProduct.Quantity
                        };

                        order.Items.Add(orderItem);
                        totalAmount += orderItem.Subtotal;

                        // Update inventory
                        wholesalerProduct.StockQuantity -= selectedProduct.Quantity;
                        _context.Update(wholesalerProduct);
                    }
                }

                order.TotalAmount = totalAmount;
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Create transaction
                var transaction = new Transaction
                {
                    OrderId = order.Id,
                    Amount = totalAmount,
                    AmountPaid = 0,
                    TransactionDate = DateTime.UtcNow,
                    WholesalerId = user.Id,
                    RetailerId = null,
                    Status = TransactionStatus.Pending
                };

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"External order created successfully for {model.CustomerName}! Total: ${totalAmount:F2}";
                return RedirectToAction(nameof(Index));
            }

            // Reload products if validation failed
            var products = await _context.WholesalerProducts
                .Include(wp => wp.Product)
                .Where(wp => wp.WholesalerId == user.Id && wp.StockQuantity > 0)
                .ToListAsync();

            model.AvailableProducts = products.Select(wp => new ProductSelectionItem
            {
                ProductId = wp.Id,
                ProductName = wp.Product.Name,
                Price = wp.Price,
                AvailableStock = wp.StockQuantity
            }).ToList();

            return View(model);
        }

        // GET: ExternalRetailers/Pay/5 - Make payment for external order
        public async Task<IActionResult> Pay(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.WholesalerId == user.Id && o.RetailerId == null);

            if (order == null) return NotFound();

            var transaction = await _context.Transactions
                .Include(t => t.Payments)
                .FirstOrDefaultAsync(t => t.OrderId == order.Id);

            if (transaction == null) return NotFound();

            var amountPaid = transaction.Payments?.Sum(p => p.Amount) ?? 0;
            var outstanding = transaction.Amount - amountPaid;

            // Extract customer name
            string customerName = "Unknown Customer";
            if (!string.IsNullOrEmpty(order.Notes) && order.Notes.StartsWith("External Order for: "))
            {
                customerName = order.Notes.Split('\n')[0].Replace("External Order for: ", "").Trim();
            }

            var viewModel = new ExternalPaymentViewModel
            {
                OrderId = order.Id,
                TransactionId = transaction.Id,
                CustomerName = customerName,
                TotalAmount = transaction.Amount,
                AmountPaid = amountPaid,
                Outstanding = outstanding,
                PaymentAmount = outstanding, // Default to full payment
                PaymentMethod = PaymentMethod.Cash
            };

            return View(viewModel);
        }

        // POST: ExternalRetailers/Pay
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(ExternalPaymentViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (ModelState.IsValid)
            {
                var transaction = await _context.Transactions
                    .Include(t => t.Payments)
                    .FirstOrDefaultAsync(t => t.Id == model.TransactionId);

                if (transaction == null) return NotFound();

                var currentPaid = transaction.Payments?.Sum(p => p.Amount) ?? 0;
                
                if (model.PaymentAmount > (transaction.Amount - currentPaid))
                {
                    ModelState.AddModelError("PaymentAmount", "Payment amount cannot exceed outstanding balance.");
                    return View(model);
                }

                // Create payment
                var payment = new Payment
                {
                    TransactionId = transaction.Id,
                    Amount = model.PaymentAmount,
                    PaymentDate = DateTime.UtcNow,
                    Method = model.PaymentMethod,
                    ReferenceNumber = $"EXT-{DateTime.UtcNow:yyyyMMdd}-{transaction.Id}",
                    Notes = model.Notes ?? $"Payment for external order by {model.CustomerName}"
                };

                _context.Payments.Add(payment);

                // Update transaction
                var newAmountPaid = currentPaid + model.PaymentAmount;
                transaction.AmountPaid = newAmountPaid;
                
                if (newAmountPaid >= transaction.Amount)
                {
                    transaction.Status = TransactionStatus.Completed;
                }
                else if (newAmountPaid > 0)
                {
                    transaction.Status = TransactionStatus.PartiallyPaid;
                }

                _context.Update(transaction);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Payment of ${model.PaymentAmount:F2} recorded successfully for {model.CustomerName}!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // POST: ExternalRetailers/PayFull/5 - Quick full payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayFull(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var transaction = await _context.Transactions
                .Include(t => t.Payments)
                .Include(t => t.Order)
                .FirstOrDefaultAsync(t => t.OrderId == id && t.Order!.WholesalerId == user.Id);

            if (transaction == null) return NotFound();

            var amountPaid = transaction.Payments?.Sum(p => p.Amount) ?? 0;
            var outstanding = transaction.Amount - amountPaid;

            if (outstanding <= 0)
            {
                TempData["Warning"] = "This order is already fully paid.";
                return RedirectToAction(nameof(Index));
            }

            // Extract customer name
            string customerName = "Unknown Customer";
            if (!string.IsNullOrEmpty(transaction.Order?.Notes) && transaction.Order.Notes.StartsWith("External Order for: "))
            {
                customerName = transaction.Order.Notes.Split('\n')[0].Replace("External Order for: ", "").Trim();
            }

            // Create full payment
            var payment = new Payment
            {
                TransactionId = transaction.Id,
                Amount = outstanding,
                PaymentDate = DateTime.UtcNow,
                Method = PaymentMethod.Cash,
                ReferenceNumber = $"EXT-FULL-{DateTime.UtcNow:yyyyMMdd}-{transaction.Id}",
                Notes = $"Full payment for external order - {customerName}"
            };

            _context.Payments.Add(payment);

            // Complete transaction
            transaction.AmountPaid = transaction.Amount;
            transaction.Status = TransactionStatus.Completed;
            _context.Update(transaction);

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Full payment of ${outstanding:F2} completed for {customerName}!";
            return RedirectToAction(nameof(Index));
        }
    }
} 