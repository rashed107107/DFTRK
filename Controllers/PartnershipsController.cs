using DFTRK.Data;
using DFTRK.Models;
using DFTRK.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DFTRK.Controllers
{
    [Authorize(Roles = "Retailer")]
    public class PartnershipsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PartnershipsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Partnerships
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var partnerships = await _context.RetailerPartnerships
                .Include(rp => rp.Categories)
                .Include(rp => rp.Products)
                .Where(rp => rp.RetailerId == user.Id)
                .OrderBy(rp => rp.PartnershipName)
                .ToListAsync();

            var viewModel = new PartnershipIndexViewModel
            {
                Partnerships = partnerships
            };

            return View(viewModel);
        }

        // GET: Partnerships/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var partnership = await _context.RetailerPartnerships
                .Include(rp => rp.Categories)
                .Include(rp => rp.Products)
                .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(rp => rp.Id == id && rp.RetailerId == user.Id);

            if (partnership == null) return NotFound();

            var viewModel = new PartnershipDetailsViewModel
            {
                Partnership = partnership,
                Categories = partnership.Categories?.Where(c => c.IsActive).ToList() ?? new List<RetailerPartnerCategory>(),
                Products = partnership.Products?.Where(p => p.IsActive).ToList() ?? new List<RetailerPartnerProduct>(),
                TotalProducts = partnership.Products?.Count(p => p.IsActive) ?? 0,
                TotalCategories = partnership.Categories?.Count(c => c.IsActive) ?? 0
            };

            return View(viewModel);
        }

        // GET: Partnerships/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new PartnershipCreateViewModel();
            return View(viewModel);
        }

        // POST: Partnerships/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PartnershipCreateViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Check if partnership already exists with the same name
            var existingPartnership = await _context.RetailerPartnerships
                .FirstOrDefaultAsync(rp => rp.RetailerId == user.Id && 
                                          rp.PartnershipName.ToLower() == viewModel.PartnershipName.ToLower());

            if (existingPartnership != null)
            {
                ModelState.AddModelError("PartnershipName", "Partnership with this name already exists.");
                return View(viewModel);
            }

            var partnership = new RetailerPartnership
            {
                RetailerId = user.Id,
                WholesalerName = viewModel.PartnershipName, // Use partnership name as wholesaler name
                PartnershipName = viewModel.PartnershipName,
                Notes = viewModel.Notes,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };

            _context.RetailerPartnerships.Add(partnership);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Partnership created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Partnerships/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var partnership = await _context.RetailerPartnerships
                .FirstOrDefaultAsync(rp => rp.Id == id && rp.RetailerId == user.Id);

            if (partnership == null) return NotFound();

            var viewModel = new PartnershipEditViewModel
            {
                Id = partnership.Id,
                PartnershipName = partnership.PartnershipName,
                Notes = partnership.Notes,
                IsActive = partnership.IsActive
            };

            return View(viewModel);
        }

        // POST: Partnerships/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PartnershipEditViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();

            if (!ModelState.IsValid) return View(viewModel);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var partnership = await _context.RetailerPartnerships
                .FirstOrDefaultAsync(rp => rp.Id == id && rp.RetailerId == user.Id);

            if (partnership == null) return NotFound();

            partnership.PartnershipName = viewModel.PartnershipName;
            partnership.Notes = viewModel.Notes;
            partnership.IsActive = viewModel.IsActive;

            _context.Update(partnership);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Partnership updated successfully!";
            return RedirectToAction(nameof(Details), new { id = partnership.Id });
        }

        // GET: Partnerships/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var partnership = await _context.RetailerPartnerships
                .Include(rp => rp.Categories)
                .Include(rp => rp.Products)
                .FirstOrDefaultAsync(rp => rp.Id == id && rp.RetailerId == user.Id);

            if (partnership == null) return NotFound();

            return View(partnership);
        }

        // POST: Partnerships/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var partnership = await _context.RetailerPartnerships
                .FirstOrDefaultAsync(rp => rp.Id == id && rp.RetailerId == user.Id);

            if (partnership != null)
            {
                _context.RetailerPartnerships.Remove(partnership);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Partnership deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        #region Category Management

        // GET: Partnerships/CreateCategory/5
        public async Task<IActionResult> CreateCategory(int? partnershipId)
        {
            if (partnershipId == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var partnership = await _context.RetailerPartnerships
                .FirstOrDefaultAsync(rp => rp.Id == partnershipId && rp.RetailerId == user.Id);

            if (partnership == null) return NotFound();

            var viewModel = new CategoryCreateViewModel
            {
                PartnershipId = partnership.Id,
                PartnershipName = partnership.PartnershipName
            };

            return View(viewModel);
        }

        // POST: Partnerships/CreateCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(CategoryCreateViewModel viewModel)
        {
            if (!ModelState.IsValid) return View(viewModel);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var partnership = await _context.RetailerPartnerships
                .FirstOrDefaultAsync(rp => rp.Id == viewModel.PartnershipId && rp.RetailerId == user.Id);

            if (partnership == null) return NotFound();

            var category = new RetailerPartnerCategory
            {
                PartnershipId = viewModel.PartnershipId,
                Name = viewModel.Name,
                Description = viewModel.Description,
                IsActive = true
            };

            _context.RetailerPartnerCategories.Add(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Category created successfully!";
            return RedirectToAction(nameof(Details), new { id = viewModel.PartnershipId });
        }

        // GET: Partnerships/EditCategory/5
        public async Task<IActionResult> EditCategory(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var category = await _context.RetailerPartnerCategories
                .Include(c => c.Partnership)
                .FirstOrDefaultAsync(c => c.Id == id && c.Partnership.RetailerId == user.Id);

            if (category == null) return NotFound();

            var viewModel = new CategoryEditViewModel
            {
                Id = category.Id,
                PartnershipId = category.PartnershipId,
                PartnershipName = category.Partnership.PartnershipName,
                Name = category.Name,
                Description = category.Description,
                IsActive = category.IsActive
            };

            return View(viewModel);
        }

        // POST: Partnerships/EditCategory/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, CategoryEditViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();
            if (!ModelState.IsValid) return View(viewModel);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var category = await _context.RetailerPartnerCategories
                .Include(c => c.Partnership)
                .FirstOrDefaultAsync(c => c.Id == id && c.Partnership.RetailerId == user.Id);

            if (category == null) return NotFound();

            category.Name = viewModel.Name;
            category.Description = viewModel.Description;
            category.IsActive = viewModel.IsActive;

            _context.Update(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Category updated successfully!";
            return RedirectToAction(nameof(Details), new { id = category.PartnershipId });
        }

        // POST: Partnerships/DeleteCategory/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var category = await _context.RetailerPartnerCategories
                .Include(c => c.Partnership)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id && c.Partnership.RetailerId == user.Id);

            if (category == null) return NotFound();

            var partnershipId = category.PartnershipId;

            // Check if category has products
            if (category.Products?.Any() == true)
            {
                TempData["Error"] = "Cannot delete category that contains products. Please remove or reassign products first.";
                return RedirectToAction(nameof(Details), new { id = partnershipId });
            }

            _context.RetailerPartnerCategories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Category deleted successfully!";
            return RedirectToAction(nameof(Details), new { id = partnershipId });
        }

        #endregion

        #region Product Management

        // GET: Partnerships/CreateProduct/5
        public async Task<IActionResult> CreateProduct(int? partnershipId)
        {
            if (partnershipId == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var partnership = await _context.RetailerPartnerships
                .Include(rp => rp.Categories)
                .FirstOrDefaultAsync(rp => rp.Id == partnershipId && rp.RetailerId == user.Id);

            if (partnership == null) return NotFound();

            var viewModel = new ProductCreateViewModel
            {
                PartnershipId = partnership.Id,
                PartnershipName = partnership.PartnershipName,
                CategoriesList = new SelectList(partnership.Categories?.Where(c => c.IsActive) ?? new List<RetailerPartnerCategory>(), "Id", "Name")
            };

            return View(viewModel);
        }

        // POST: Partnerships/CreateProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(ProductCreateViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                var partnership = await _context.RetailerPartnerships
                    .Include(rp => rp.Categories)
                    .FirstOrDefaultAsync(rp => rp.Id == viewModel.PartnershipId);
                viewModel.CategoriesList = new SelectList(partnership?.Categories?.Where(c => c.IsActive) ?? new List<RetailerPartnerCategory>(), "Id", "Name");
                return View(viewModel);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var partnershipCheck = await _context.RetailerPartnerships
                .FirstOrDefaultAsync(rp => rp.Id == viewModel.PartnershipId && rp.RetailerId == user.Id);

            if (partnershipCheck == null) return NotFound();

            var product = new RetailerPartnerProduct
            {
                PartnershipId = viewModel.PartnershipId,
                CategoryId = viewModel.CategoryId,
                Name = viewModel.Name,
                Description = viewModel.Description,
                CostPrice = viewModel.PurchasePrice,
                SellingPrice = viewModel.PurchasePrice, // Default selling price to purchase price
                StockQuantity = 0, // Default to 0 since not used
                MinimumStock = 0, // Default to 0 since not used
                SKU = null, // No longer collected
                IsActive = true
            };

            _context.RetailerPartnerProducts.Add(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Product created successfully!";
            return RedirectToAction(nameof(Details), new { id = viewModel.PartnershipId });
        }

        // GET: Partnerships/EditProduct/5
        public async Task<IActionResult> EditProduct(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var product = await _context.RetailerPartnerProducts
                .Include(p => p.Partnership)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id && p.Partnership.RetailerId == user.Id);

            if (product == null) return NotFound();

            var categories = await _context.RetailerPartnerCategories
                .Where(c => c.PartnershipId == product.PartnershipId && c.IsActive)
                .ToListAsync();

            var viewModel = new ProductEditViewModel
            {
                Id = product.Id,
                PartnershipId = product.PartnershipId,
                PartnershipName = product.Partnership.PartnershipName,
                CategoryId = product.CategoryId,
                Name = product.Name,
                Description = product.Description,
                PurchasePrice = product.CostPrice,
                IsActive = product.IsActive,
                CategoriesList = new SelectList(categories, "Id", "Name", product.CategoryId)
            };

            return View(viewModel);
        }

        // POST: Partnerships/EditProduct/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id, ProductEditViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                var categories = await _context.RetailerPartnerCategories
                    .Where(c => c.PartnershipId == viewModel.PartnershipId && c.IsActive)
                    .ToListAsync();
                viewModel.CategoriesList = new SelectList(categories, "Id", "Name", viewModel.CategoryId);
                return View(viewModel);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var product = await _context.RetailerPartnerProducts
                .Include(p => p.Partnership)
                .FirstOrDefaultAsync(p => p.Id == id && p.Partnership.RetailerId == user.Id);

            if (product == null) return NotFound();

            product.CategoryId = viewModel.CategoryId;
            product.Name = viewModel.Name;
            product.Description = viewModel.Description;
            product.CostPrice = viewModel.PurchasePrice;
            product.SellingPrice = viewModel.PurchasePrice; // Keep selling price same as purchase price
            product.IsActive = viewModel.IsActive;

            _context.Update(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Product updated successfully!";
            return RedirectToAction(nameof(Details), new { id = product.PartnershipId });
        }

        // GET: Partnerships/DeleteProduct/5
        public async Task<IActionResult> DeleteProduct(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var product = await _context.RetailerPartnerProducts
                .Include(p => p.Partnership)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id && p.Partnership.RetailerId == user.Id);

            if (product == null) return NotFound();

            return View(product);
        }

        // POST: Partnerships/DeleteProduct/5
        [HttpPost, ActionName("DeleteProduct")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProductConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var product = await _context.RetailerPartnerProducts
                .Include(p => p.Partnership)
                .FirstOrDefaultAsync(p => p.Id == id && p.Partnership.RetailerId == user.Id);

            if (product == null) return NotFound();

            var partnershipId = product.PartnershipId;

            _context.RetailerPartnerProducts.Remove(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Product deleted successfully!";
            return RedirectToAction(nameof(Details), new { id = partnershipId });
        }

        #endregion

        #region Product Ordering

        // GET: Partnerships/OrderProducts/5
        public async Task<IActionResult> OrderProducts(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var partnership = await _context.RetailerPartnerships
                .Include(rp => rp.Products)
                .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(rp => rp.Id == id && rp.RetailerId == user.Id && rp.IsActive);

            if (partnership == null) return NotFound();

            // Since wholesalers are not users in the system, we'll show sample products
            // This would be replaced with actual integration to wholesaler's catalog
            var wholesalerProducts = new List<WholesalerProduct>(); // Empty list for now

            var viewModel = new ProductOrderViewModel
            {
                Partnership = partnership,
                WholesalerProducts = wholesalerProducts,
                OrderItems = new List<OrderItemViewModel>()
            };

            return View(viewModel);
        }

        // POST: Partnerships/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(ProductOrderViewModel viewModel)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var partnership = await _context.RetailerPartnerships
                .FirstOrDefaultAsync(rp => rp.Id == viewModel.Partnership.Id && rp.RetailerId == user.Id && rp.IsActive);

            if (partnership == null) return NotFound();

            // Validate order items
            var validOrderItems = viewModel.OrderItems?.Where(oi => oi.Quantity > 0).ToList() ?? new List<OrderItemViewModel>();

            if (!validOrderItems.Any())
            {
                TempData["Error"] = "Please select at least one product to order.";
                return RedirectToAction(nameof(OrderProducts), new { id = partnership.Id });
            }

            // Since wholesalers are not in the system, this would be handled differently
            // For now, we'll just show a message
            TempData["Info"] = $"Order request sent to {partnership.WholesalerName}. You will be contacted to finalize the order.";
            return RedirectToAction(nameof(Details), new { id = partnership.Id });
        }

        #endregion

        #region Order Management

        // GET: Partnerships/CreateOrder/5
        public async Task<IActionResult> CreateOrder(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var partnership = await _context.RetailerPartnerships
                .Include(rp => rp.Products)
                .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(rp => rp.Id == id && rp.RetailerId == user.Id && rp.IsActive);

            if (partnership == null) return NotFound();

            var availableProducts = partnership.Products?.Where(p => p.IsActive).ToList() ?? new List<RetailerPartnerProduct>();

            var viewModel = new PartnerOrderCreateViewModel
            {
                PartnershipId = partnership.Id,
                PartnershipName = partnership.PartnershipName,
                WholesalerName = partnership.WholesalerName,
                AvailableProducts = availableProducts,
                OrderItems = availableProducts.Select(p => new PartnerOrderItemViewModel
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    PurchasePrice = p.CostPrice,
                    Quantity = 0,
                    IsSelected = false
                }).ToList()
            };

            return View(viewModel);
        }

        // POST: Partnerships/CreateOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder(PartnerOrderCreateViewModel viewModel)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var partnership = await _context.RetailerPartnerships
                .Include(rp => rp.Products)
                .FirstOrDefaultAsync(rp => rp.Id == viewModel.PartnershipId && rp.RetailerId == user.Id && rp.IsActive);

            if (partnership == null) return NotFound();

            // Validate selected items
            var selectedItems = viewModel.OrderItems?.Where(oi => oi.IsSelected && oi.Quantity > 0).ToList() ?? new List<PartnerOrderItemViewModel>();

            if (!selectedItems.Any())
            {
                ModelState.AddModelError("", "Please select at least one product to order.");
                viewModel.AvailableProducts = partnership.Products?.Where(p => p.IsActive).ToList() ?? new List<RetailerPartnerProduct>();
                return View(viewModel);
            }

            // Create the order
            var order = new Order
            {
                RetailerId = user.Id,
                WholesalerId = null, // Partnership orders don't have wholesaler users
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                TotalAmount = selectedItems.Sum(item => item.Subtotal),
                Notes = $"Partnership Order from: {partnership.PartnershipName}" + 
                       (string.IsNullOrEmpty(viewModel.Notes) ? "" : $"\nNotes: {viewModel.Notes}")
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Create order items
            foreach (var item in selectedItems)
            {
                var product = partnership.Products?.FirstOrDefault(p => p.Id == item.ProductId);
                if (product != null)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        WholesalerProductId = null, // Partnership orders don't reference wholesaler products
                        PartnerProductId = product.Id, // Store partner product ID
                        ProductName = product.Name, // Store product name for display
                        Quantity = item.Quantity,
                        UnitPrice = product.CostPrice,
                        Subtotal = item.Subtotal
                    };

                    _context.OrderItems.Add(orderItem);
                }
            }

            // Create transaction record for payment
            var transaction = new Transaction
            {
                RetailerId = user.Id,
                WholesalerId = partnership.WholesalerName, // Use partnership name as identifier
                Amount = order.TotalAmount,
                AmountPaid = 0, // No payment made yet
                TransactionDate = DateTime.UtcNow,
                Status = TransactionStatus.Pending,
                Order = order,
                PaymentMethod = "Partnership Order", // Indicate this is a partnership transaction
                TransactionReference = $"PARTNER-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8)}"
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Order created successfully! You can now proceed to payment.";
            return RedirectToAction("Details", "Payments", new { id = transaction.Id });
        }

        // GET: Partnerships/PartnerOrders/5
        public async Task<IActionResult> PartnerOrders(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var partnership = await _context.RetailerPartnerships
                .FirstOrDefaultAsync(rp => rp.Id == id && rp.RetailerId == user.Id);

            if (partnership == null) return NotFound();

            // Get orders for this partnership (partnership orders have null WholesalerId and partnership name in Notes)
            var orders = await _context.Orders
                .Where(o => o.RetailerId == user.Id && 
                           o.WholesalerId == null && 
                           o.Notes != null && 
                           o.Notes.Contains($"Partnership Order from: {partnership.PartnershipName}"))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var viewModel = new PartnerOrderIndexViewModel
            {
                Orders = orders,
                PartnershipId = partnership.Id,
                PartnershipName = partnership.PartnershipName
            };

            return View(viewModel);
        }

        // GET: Partnerships/OrderDetails/5
        public async Task<IActionResult> OrderDetails(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id && o.RetailerId == user.Id);

            if (order == null) return NotFound();

            // Find the partnership for this order (check if it's a partnership order with null WholesalerId)
            RetailerPartnership? partnership = null;
            if (order.WholesalerId == null && order.Notes != null && order.Notes.StartsWith("Partnership Order from: "))
            {
                // Extract partnership name from notes
                var partnershipName = order.Notes.Substring("Partnership Order from: ".Length).Split('\n')[0];
                partnership = await _context.RetailerPartnerships
                    .FirstOrDefaultAsync(rp => rp.RetailerId == user.Id && rp.PartnershipName == partnershipName);
            }

            var viewModel = new PartnerOrderDetailsViewModel
            {
                Order = order,
                Partnership = partnership ?? new RetailerPartnership(),
                OrderItems = order.Items?.ToList() ?? new List<OrderItem>(),
                TotalAmount = order.TotalAmount
            };

            return View(viewModel);
        }

        #endregion

        #region Partnership Order Status Management

        // POST: Partnerships/ProcessOrder/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessOrder(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Get the order and verify it's a partnership order for this retailer
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && 
                                         o.RetailerId == user.Id && 
                                         o.WholesalerId == null && 
                                         o.Status == OrderStatus.Pending);

            if (order == null) return NotFound();

            // Update order status to Processing
            order.Status = OrderStatus.Processing;
            _context.Update(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Partnership order has been processed and is ready for shipment.";
            return RedirectToAction("OrderDetails", new { id = order.Id });
        }

        // POST: Partnerships/ShipOrder/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShipOrder(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Get the order and verify it's a partnership order for this retailer
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && 
                                         o.RetailerId == user.Id && 
                                         o.WholesalerId == null && 
                                         o.Status == OrderStatus.Processing);

            if (order == null) return NotFound();

            // Update order status to Shipped
            order.Status = OrderStatus.Shipped;
            _context.Update(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Partnership order has been marked as shipped. You can now confirm delivery.";
            return RedirectToAction("OrderDetails", new { id = order.Id });
        }

        #endregion
    }
} 