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
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Payments/Details/5 (transactionId)
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions
                .Include(t => t.Order)
                .Include(t => t.Payments)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
            {
                return NotFound();
            }

            // Check if user is authorized to view this transaction
            // For external orders (RetailerId is null), only the wholesaler and admin can access
            bool isAuthorized = User.IsInRole("Admin") || 
                               transaction.WholesalerId == user.Id || 
                               (transaction.RetailerId != null && transaction.RetailerId == user.Id);
            
            if (!isAuthorized)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Update transaction AmountPaid to ensure accuracy
            var actualAmountPaid = transaction.Payments?.Sum(p => p.Amount) ?? 0;
            if (transaction.AmountPaid != actualAmountPaid)
            {
                transaction.AmountPaid = actualAmountPaid;
                
                // Update transaction status
                if (transaction.AmountPaid >= transaction.Amount)
                {
                    transaction.Status = TransactionStatus.Completed;
                }
                else if (transaction.AmountPaid > 0)
                {
                    transaction.Status = TransactionStatus.PartiallyPaid;
                }
                else
                {
                    transaction.Status = TransactionStatus.Pending;
                }
                
                _context.Update(transaction);
                await _context.SaveChangesAsync();
            }

            // Get wholesaler name - handle both regular and partnership orders
            string wholesalerName;
            if (transaction.PaymentMethod == "Partnership Order")
            {
                // For partnership orders, WholesalerId contains the partnership name
                wholesalerName = transaction.WholesalerId;
            }
            else
            {
                // For regular orders, WholesalerId is a user ID
                var wholesaler = await _userManager.FindByIdAsync(transaction.WholesalerId);
                wholesalerName = wholesaler?.BusinessName ?? "Unknown";
            }

            var viewModel = new PaymentViewModel
            {
                TransactionId = transaction.Id,
                OrderId = transaction.OrderId,
                OrderReference = $"Order #{transaction.OrderId}",
                WholesalerName = wholesalerName,
                TotalAmount = transaction.Amount,
                AmountPaid = transaction.AmountPaid,
                RemainingAmount = transaction.Amount - transaction.AmountPaid,
                Status = transaction.Status,
                OrderStatus = transaction.Order?.Status ?? OrderStatus.Pending,
                PaymentHistory = transaction.Payments?.Select(p => new PaymentItemViewModel
                {
                    Id = p.Id,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    Method = p.Method,
                    ReferenceNumber = p.ReferenceNumber,
                    Notes = p.Notes
                }).OrderByDescending(p => p.PaymentDate).ToList()
            };

            return View(viewModel);
        }

        // GET: Payments/MakePayment/5 (transactionId)
        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> MakePayment(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions
                .Include(t => t.Order)
                .Include(t => t.Payments)
                .FirstOrDefaultAsync(t => t.Id == id && t.RetailerId == user.Id);

            if (transaction == null)
            {
                return NotFound();
            }

            // Check if the order is cancelled - prevent payment for cancelled orders
            if (transaction.Order != null && transaction.Order.Status == OrderStatus.Cancelled)
            {
                TempData["Error"] = "Cannot make payment for a cancelled order.";
                return RedirectToAction("Details", new { id = transaction.Id });
            }

            // Update transaction AmountPaid to ensure accuracy
            var actualAmountPaid = transaction.Payments?.Sum(p => p.Amount) ?? 0;
            if (transaction.AmountPaid != actualAmountPaid)
            {
                transaction.AmountPaid = actualAmountPaid;
                
                // Update transaction status
                if (transaction.AmountPaid >= transaction.Amount)
                {
                    transaction.Status = TransactionStatus.Completed;
                }
                else if (transaction.AmountPaid > 0)
                {
                    transaction.Status = TransactionStatus.PartiallyPaid;
                }
                else
                {
                    transaction.Status = TransactionStatus.Pending;
                }
                
                _context.Update(transaction);
                await _context.SaveChangesAsync();
            }

            // Get wholesaler name - handle both regular and partnership orders
            string wholesalerName;
            if (transaction.PaymentMethod == "Partnership Order")
            {
                // For partnership orders, WholesalerId contains the partnership name
                wholesalerName = transaction.WholesalerId;
            }
            else
            {
                // For regular orders, WholesalerId is a user ID
                var wholesaler = await _userManager.FindByIdAsync(transaction.WholesalerId);
                wholesalerName = wholesaler?.BusinessName ?? "Unknown";
            }

            var viewModel = new MakePaymentViewModel
            {
                TransactionId = transaction.Id,
                OrderId = transaction.OrderId,
                OrderReference = $"Order #{transaction.OrderId}",
                TotalAmount = transaction.Amount,
                AmountPaid = transaction.AmountPaid,
                RemainingAmount = transaction.Amount - transaction.AmountPaid,
                PaymentAmount = transaction.Amount - transaction.AmountPaid // Default to full remaining amount
            };

            return View(viewModel);
        }

        // POST: Payments/MakePayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> MakePayment(MakePaymentViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions
                .Include(t => t.Order)
                .Include(t => t.Payments)
                .FirstOrDefaultAsync(t => t.Id == model.TransactionId && t.RetailerId == user.Id);

            if (transaction == null)
            {
                return NotFound();
            }
            
            // Check if the order is cancelled - prevent payment for cancelled orders
            if (transaction.Order != null && transaction.Order.Status == OrderStatus.Cancelled)
            {
                TempData["Error"] = "Cannot make payment for a cancelled order.";
                return RedirectToAction("Details", new { id = transaction.Id });
            }

            if (ModelState.IsValid)
            {
                // Calculate current amount paid from actual payments
                var currentAmountPaid = transaction.Payments?.Sum(p => p.Amount) ?? 0;
                
                // Validate payment amount doesn't exceed remaining balance
                if (model.PaymentAmount > (transaction.Amount - currentAmountPaid))
                {
                    ModelState.AddModelError("PaymentAmount", "Payment amount cannot exceed the remaining balance.");
                    return View(model);
                }

                // Create payment record
                var payment = new Payment
                {
                    TransactionId = transaction.Id,
                    Amount = model.PaymentAmount,
                    PaymentDate = DateTime.UtcNow,
                    Method = model.PaymentMethod,
                    ReferenceNumber = model.ReferenceNumber,
                    Notes = model.Notes
                };

                _context.Payments.Add(payment);

                // Recalculate AmountPaid from payments (including the new one)
                var totalAmountPaid = currentAmountPaid + model.PaymentAmount;
                transaction.AmountPaid = totalAmountPaid;
                
                // Update transaction status
                if (transaction.AmountPaid >= transaction.Amount)
                {
                    transaction.Status = TransactionStatus.Completed;
                }
                else if (transaction.AmountPaid > 0)
                {
                    transaction.Status = TransactionStatus.PartiallyPaid;
                }

                _context.Update(transaction);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Payment of ${model.PaymentAmount:F2} has been successfully processed.";
                return RedirectToAction("Details", new { id = transaction.Id });
            }

            return View(model);
        }

        // GET: Payments
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            IQueryable<Transaction> transactionsQuery;
            
            if (User.IsInRole("Retailer"))
            {
                // Retailers see their outgoing payments
                transactionsQuery = _context.Transactions
                    .Include(t => t.Order)
                    .Include(t => t.Payments)
                    .Where(t => t.RetailerId == user.Id && t.Order.Status != OrderStatus.Cancelled)
                    .OrderByDescending(t => t.TransactionDate);
            }
            else if (User.IsInRole("Wholesaler"))
            {
                // Wholesalers see their incoming payments
                transactionsQuery = _context.Transactions
                    .Include(t => t.Order)
                    .Include(t => t.Payments)
                    .Where(t => t.WholesalerId == user.Id && t.Order.Status != OrderStatus.Cancelled)
                    .OrderByDescending(t => t.TransactionDate);
            }
            else if (User.IsInRole("Admin"))
            {
                // Admins see all payments
                transactionsQuery = _context.Transactions
                    .Include(t => t.Order)
                    .Include(t => t.Payments)
                    .Where(t => t.Order.Status != OrderStatus.Cancelled)
                    .OrderByDescending(t => t.TransactionDate);
            }
            else
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var transactions = await transactionsQuery.ToListAsync();

            // Filter out transactions associated with cancelled orders
            transactions = transactions.Where(t => t.Order?.Status != OrderStatus.Cancelled).ToList();

            // Update transaction AmountPaid for all transactions to ensure accuracy
            foreach (var transaction in transactions)
            {
                var actualAmountPaid = transaction.Payments?.Sum(p => p.Amount) ?? 0;
                if (transaction.AmountPaid != actualAmountPaid)
                {
                    transaction.AmountPaid = actualAmountPaid;
                    
                    // Update transaction status
                    if (transaction.AmountPaid >= transaction.Amount)
                    {
                        transaction.Status = TransactionStatus.Completed;
                    }
                    else if (transaction.AmountPaid > 0)
                    {
                        transaction.Status = TransactionStatus.PartiallyPaid;
                    }
                    else
                    {
                        transaction.Status = TransactionStatus.Pending;
                    }
                    
                    _context.Update(transaction);
                }
            }

            var viewModels = new List<PaymentViewModel>();

            foreach (var transaction in transactions)
            {
                // Get wholesaler name - handle both regular and partnership orders
                string wholesalerName;
                if (transaction.PaymentMethod == "Partnership Order")
                {
                    // For partnership orders, WholesalerId contains the partnership name
                    wholesalerName = transaction.WholesalerId;
                }
                else
                {
                    // For regular orders, WholesalerId is a user ID
                    var wholesaler = await _userManager.FindByIdAsync(transaction.WholesalerId);
                    wholesalerName = wholesaler?.BusinessName ?? "Unknown";
                }
                
                viewModels.Add(new PaymentViewModel
                {
                    TransactionId = transaction.Id,
                    OrderId = transaction.OrderId,
                    OrderReference = $"Order #{transaction.OrderId}",
                    WholesalerName = wholesalerName,
                    TotalAmount = transaction.Amount,
                    AmountPaid = transaction.AmountPaid,
                    RemainingAmount = transaction.Amount - transaction.AmountPaid,
                    Status = transaction.Status,
                    OrderStatus = transaction.Order?.Status ?? OrderStatus.Pending,
                    PaymentHistory = transaction.Payments?.Select(p => new PaymentItemViewModel
                    {
                        Id = p.Id,
                        Amount = p.Amount,
                        PaymentDate = p.PaymentDate,
                        Method = p.Method,
                        ReferenceNumber = p.ReferenceNumber,
                        Notes = p.Notes
                    }).OrderByDescending(p => p.PaymentDate).ToList()
                });
            }

            // Save any transaction updates
            await _context.SaveChangesAsync();

            return View(viewModels);
        }

        // GET: Payments/ExternalPayments - Redirect to new simplified external orders
        [Authorize(Roles = "Wholesaler")]
        public IActionResult ExternalPayments()
        {
            // Redirect to the new simplified external orders controller
            return RedirectToAction("Index", "ExternalRetailers");
        }

        // GET: Payments/RecordExternalPayment/5 (transactionId)
        [Authorize(Roles = "Wholesaler")]
        public async Task<IActionResult> RecordExternalPayment(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var transaction = await _context.Transactions
                .Include(t => t.Order)
                .Include(t => t.Payments)
                .FirstOrDefaultAsync(t => t.Id == id && 
                                   t.Order!.WholesalerId == user.Id && 
                                   t.Order.RetailerId == null); // External orders only

            if (transaction == null) return NotFound();

            // Update transaction AmountPaid to ensure accuracy  
            var actualAmountPaid = transaction.Payments?.Sum(p => p.Amount) ?? 0;
            if (transaction.AmountPaid != actualAmountPaid)
            {
                transaction.AmountPaid = actualAmountPaid;
                
                // Update transaction status
                if (transaction.AmountPaid >= transaction.Amount)
                {
                    transaction.Status = TransactionStatus.Completed;
                }
                else if (transaction.AmountPaid > 0)
                {
                    transaction.Status = TransactionStatus.PartiallyPaid;
                }
                else
                {
                    transaction.Status = TransactionStatus.Pending;
                }
                
                _context.Update(transaction);
                await _context.SaveChangesAsync();
            }

            // Extract retailer name from order notes
            var retailerName = "External Customer";
            if (transaction.Order?.Notes?.StartsWith("External Order for: ") == true)
            {
                var lines = transaction.Order.Notes.Split('\n');
                retailerName = lines[0].Replace("External Order for: ", "");
            }

            var viewModel = new MakePaymentViewModel
            {
                TransactionId = transaction.Id,
                OrderId = transaction.OrderId,
                OrderReference = $"Order #{transaction.OrderId}",
                TotalAmount = transaction.Amount,
                AmountPaid = transaction.AmountPaid,
                RemainingAmount = transaction.Amount - transaction.AmountPaid,
                PaymentAmount = transaction.Amount - transaction.AmountPaid, // Default to full remaining
                WholesalerName = retailerName // Reusing this field for external retailer name
            };

            return View(viewModel);
        }

        // POST: Payments/RecordExternalPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Wholesaler")]
        public async Task<IActionResult> RecordExternalPayment(MakePaymentViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var transaction = await _context.Transactions
                .Include(t => t.Order)
                .Include(t => t.Payments)
                .FirstOrDefaultAsync(t => t.Id == model.TransactionId && 
                                   t.Order!.WholesalerId == user.Id && 
                                   t.Order.RetailerId == null); // External orders only

            if (transaction == null) return NotFound();

            if (ModelState.IsValid)
            {
                // Validate payment amount
                if (model.PaymentAmount > (transaction.Amount - transaction.AmountPaid))
                {
                    ModelState.AddModelError("PaymentAmount", "Payment amount cannot exceed the remaining balance.");
                    return View(model);
                }

                // Create payment record
                var payment = new Payment
                {
                    TransactionId = transaction.Id,
                    Amount = model.PaymentAmount,
                    PaymentDate = DateTime.UtcNow,
                    Method = model.PaymentMethod,
                    ReferenceNumber = model.ReferenceNumber,
                    Notes = model.Notes
                };

                _context.Payments.Add(payment);

                // Recalculate AmountPaid from payments (including the new one)
                var totalAmountPaid = (transaction.Payments?.Sum(p => p.Amount) ?? 0) + model.PaymentAmount;
                transaction.AmountPaid = totalAmountPaid;
                
                // Update transaction status
                if (transaction.AmountPaid >= transaction.Amount)
                {
                    transaction.Status = TransactionStatus.Completed;
                }
                else if (transaction.AmountPaid > 0)
                {
                    transaction.Status = TransactionStatus.PartiallyPaid;
                }
                else
                {
                    transaction.Status = TransactionStatus.Pending;
                }

                _context.Update(transaction);
                await _context.SaveChangesAsync();

                // Extract retailer name for success message
                var retailerName = "External Customer";
                if (transaction.Order?.Notes?.StartsWith("External Order for: ") == true)
                {
                    var lines = transaction.Order.Notes.Split('\n');
                    retailerName = lines[0].Replace("External Order for: ", "");
                }

                TempData["Success"] = $"Payment of ${model.PaymentAmount:N2} recorded successfully for {retailerName}!";
                return RedirectToAction(nameof(ExternalPayments));
            }

            return View(model);
        }

        // POST: Payments/QuickPayExternal/5 (transactionId) - Quick pay full amount for external retailer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Wholesaler")]
        public async Task<IActionResult> QuickPayExternal(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var transaction = await _context.Transactions
                .Include(t => t.Order)
                .Include(t => t.Payments)
                .FirstOrDefaultAsync(t => t.Id == id && 
                                   t.Order!.WholesalerId == user.Id && 
                                   t.Order.RetailerId == null); // External orders only

            if (transaction == null) return NotFound();

            // Calculate remaining amount
            var actualAmountPaid = transaction.Payments?.Sum(p => p.Amount) ?? 0;
            var remainingAmount = transaction.Amount - actualAmountPaid;

            if (remainingAmount <= 0)
            {
                TempData["Warning"] = "This order is already fully paid.";
                return RedirectToAction("ExternalPayments");
            }

            // Extract retailer name from order notes
            var retailerName = "External Customer";
            if (transaction.Order?.Notes?.StartsWith("External Order for: ") == true)
            {
                var lines = transaction.Order.Notes.Split('\n');
                retailerName = lines[0].Replace("External Order for: ", "").Trim();
            }

            // Create payment record for the full remaining amount
            var payment = new Payment
            {
                TransactionId = transaction.Id,
                Amount = remainingAmount,
                PaymentDate = DateTime.UtcNow,
                Method = PaymentMethod.Other, // Default for wholesaler payments on behalf
                ReferenceNumber = $"WS-{DateTime.UtcNow:yyyyMMdd}-{transaction.Id}",
                Notes = $"Payment made by wholesaler on behalf of {retailerName}"
            };

            _context.Payments.Add(payment);

            // Update transaction status to completed
            transaction.AmountPaid = transaction.Amount;
            transaction.Status = TransactionStatus.Completed;

            _context.Update(transaction);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Successfully paid ${remainingAmount:F2} on behalf of {retailerName}!";
            return RedirectToAction("ExternalPayments");
        }
    }
} 