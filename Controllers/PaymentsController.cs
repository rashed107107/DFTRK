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
            if (!User.IsInRole("Admin") && transaction.RetailerId != user.Id && transaction.WholesalerId != user.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Get wholesaler name
            var wholesaler = await _userManager.FindByIdAsync(transaction.WholesalerId);
            var wholesalerName = wholesaler?.BusinessName ?? "Unknown";

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

            // Get wholesaler name
            var wholesaler = await _userManager.FindByIdAsync(transaction.WholesalerId);
            var wholesalerName = wholesaler?.BusinessName ?? "Unknown";

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
                // Validate payment amount doesn't exceed remaining balance
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

                // Update transaction
                transaction.AmountPaid += model.PaymentAmount;
                
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

            var viewModels = new List<PaymentViewModel>();

            foreach (var transaction in transactions)
            {
                // Get retailer and wholesaler names
                var retailer = await _userManager.FindByIdAsync(transaction.RetailerId);
                var wholesaler = await _userManager.FindByIdAsync(transaction.WholesalerId);
                
                viewModels.Add(new PaymentViewModel
                {
                    TransactionId = transaction.Id,
                    OrderId = transaction.OrderId,
                    OrderReference = $"Order #{transaction.OrderId}",
                    WholesalerName = wholesaler?.BusinessName ?? "Unknown",
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

            return View(viewModels);
        }
    }
} 