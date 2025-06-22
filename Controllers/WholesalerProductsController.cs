using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using DFTRK.Data;
using DFTRK.Models;
using DFTRK.ViewModels;

namespace DFTRK.Controllers
{
    [Authorize(Roles = "Wholesaler,Admin")]
    public class WholesalerProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public WholesalerProductsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: WholesalerProducts
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            IQueryable<WholesalerProduct> productsQuery = _context.WholesalerProducts
                .Include(p => p.Product)
                .ThenInclude(p => p.Category);

            if (!User.IsInRole("Admin"))
            {
                // Non-admin wholesalers can only see their own products
                productsQuery = productsQuery.Where(p => p.WholesalerId == user.Id);
            }

            // Order by name
            productsQuery = productsQuery.OrderBy(p => p.Product.Name);

            var products = await productsQuery.ToListAsync();
            return View(products);
        }

        // GET: WholesalerProducts/Details/5
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

            var wholesalerProduct = await _context.WholesalerProducts
                .Include(p => p.Product)
                .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (wholesalerProduct == null)
            {
                return NotFound();
            }

            // Check authorization - wholesalers can only view their own products
            if (!User.IsInRole("Admin") && wholesalerProduct.WholesalerId != user.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(wholesalerProduct);
        }

        // GET: WholesalerProducts/Create
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Get only categories created by this wholesaler
            var categories = await _context.Categories
                .Where(c => c.CreatedById == user.Id)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var viewModel = new WholesalerProductCreateViewModel
            {
                Categories = new SelectList(categories, "Id", "Name"),
                Price = 0,
                StockQuantity = 0,
                IsActive = true
            };

            return View(viewModel);
        }

        // POST: WholesalerProducts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WholesalerProductCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }

                string? imageUrl = null;
                
                // Handle image upload
                if (viewModel.ImageFile != null)
                {
                    imageUrl = await SaveImageFile(viewModel.ImageFile);
                }

                // Create new product
                var product = new Product
                {
                    Name = viewModel.Name,
                    Description = viewModel.Description,
                    CategoryId = viewModel.CategoryId,
                    ImageUrl = imageUrl
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Create wholesaler product
                var wholesalerProduct = new WholesalerProduct
                {
                    ProductId = product.Id,
                    WholesalerId = user.Id,
                    Price = viewModel.Price,
                    StockQuantity = viewModel.StockQuantity,
                    IsActive = viewModel.IsActive
                };

                _context.WholesalerProducts.Add(wholesalerProduct);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            // If we got this far, something failed, redisplay form
            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            viewModel.Categories = new SelectList(categories, "Id", "Name", viewModel.CategoryId);
            
            return View(viewModel);
        }

        // GET: WholesalerProducts/Edit/5
        public async Task<IActionResult> Edit(int? id)
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

            var wholesalerProduct = await _context.WholesalerProducts
                .Include(p => p.Product)
                .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (wholesalerProduct == null)
            {
                return NotFound();
            }

            // Check authorization - wholesalers can only edit their own products
            if (!User.IsInRole("Admin") && wholesalerProduct.WholesalerId != user.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Get only categories created by this wholesaler
            var categories = await _context.Categories
                .Where(c => c.CreatedById == user.Id)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var viewModel = new WholesalerProductEditViewModel
            {
                Id = wholesalerProduct.Id,
                ProductId = wholesalerProduct.ProductId,
                Name = wholesalerProduct.Product.Name,
                Description = wholesalerProduct.Product.Description,
                CategoryId = wholesalerProduct.Product.CategoryId,
                ImageUrl = wholesalerProduct.Product.ImageUrl,
                Price = wholesalerProduct.Price,
                StockQuantity = wholesalerProduct.StockQuantity,
                IsActive = wholesalerProduct.IsActive
            };

            viewModel.Categories = new SelectList(categories, "Id", "Name", viewModel.CategoryId);

            return View(viewModel);
        }

        // POST: WholesalerProducts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, WholesalerProductEditViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user == null)
                    {
                        return NotFound();
                    }

                    var wholesalerProduct = await _context.WholesalerProducts
                        .Include(p => p.Product)
                        .FirstOrDefaultAsync(p => p.Id == id);

                    if (wholesalerProduct == null)
                    {
                        return NotFound();
                    }

                    // Check authorization - wholesalers can only edit their own products
                    if (!User.IsInRole("Admin") && wholesalerProduct.WholesalerId != user.Id)
                    {
                        return RedirectToAction("AccessDenied", "Account");
                    }

                    // Update product data
                    wholesalerProduct.Product.Name = viewModel.Name;
                    wholesalerProduct.Product.Description = viewModel.Description;
                    wholesalerProduct.Product.CategoryId = viewModel.CategoryId;
                    
                    // Handle image upload
                    if (viewModel.ImageFile != null)
                    {
                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(wholesalerProduct.Product.ImageUrl))
                        {
                            DeleteImageFile(wholesalerProduct.Product.ImageUrl);
                        }
                        
                        // Save new image
                        wholesalerProduct.Product.ImageUrl = await SaveImageFile(viewModel.ImageFile);
                    }
                    else
                    {
                        // Keep existing image URL if no new file uploaded
                        wholesalerProduct.Product.ImageUrl = viewModel.ImageUrl;
                    }

                    // Update wholesaler product data
                    wholesalerProduct.Price = viewModel.Price;
                    wholesalerProduct.StockQuantity = viewModel.StockQuantity;
                    wholesalerProduct.IsActive = viewModel.IsActive;

                    _context.Update(wholesalerProduct.Product);
                    _context.Update(wholesalerProduct);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WholesalerProductExists(viewModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            var categories = await _context.Categories.ToListAsync();
            viewModel.Categories = new SelectList(categories, "Id", "Name", viewModel.CategoryId);
            
            return View(viewModel);
        }

        // GET: WholesalerProducts/UpdateStock/5
        public async Task<IActionResult> UpdateStock(int? id)
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

            var wholesalerProduct = await _context.WholesalerProducts
                .Include(p => p.Product)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (wholesalerProduct == null)
            {
                return NotFound();
            }

            // Check authorization - wholesalers can only update their own products
            if (!User.IsInRole("Admin") && wholesalerProduct.WholesalerId != user.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var viewModel = new WholesalerProductStockViewModel
            {
                Id = wholesalerProduct.Id,
                ProductName = wholesalerProduct.Product.Name,
                CurrentStock = wholesalerProduct.StockQuantity,
                QuantityToAdd = 0
            };

            return View(viewModel);
        }

        // POST: WholesalerProducts/UpdateStock/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(int id, WholesalerProductStockViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }

                var wholesalerProduct = await _context.WholesalerProducts
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (wholesalerProduct == null)
                {
                    return NotFound();
                }

                // Check authorization - wholesalers can only update their own products
                if (!User.IsInRole("Admin") && wholesalerProduct.WholesalerId != user.Id)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }

                // Update stock
                wholesalerProduct.StockQuantity += viewModel.QuantityToAdd;
                
                // Ensure stock doesn't go below 0
                if (wholesalerProduct.StockQuantity < 0)
                {
                    wholesalerProduct.StockQuantity = 0;
                }

                _context.Update(wholesalerProduct);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Details), new { id = wholesalerProduct.Id });
            }

            return View(viewModel);
        }

        // GET: WholesalerProducts/Delete/5
        public async Task<IActionResult> Delete(int? id)
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

            var wholesalerProduct = await _context.WholesalerProducts
                .Include(wp => wp.Product)
                    .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(wp => wp.Id == id && wp.WholesalerId == user.Id);

            if (wholesalerProduct == null)
            {
                return NotFound();
            }

            return View(wholesalerProduct);
        }

        // POST: WholesalerProducts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var wholesalerProduct = await _context.WholesalerProducts
                .FirstOrDefaultAsync(wp => wp.Id == id && wp.WholesalerId == user.Id);

            if (wholesalerProduct != null)
            {
                // Check if the product is in any orders
                var isInOrder = await _context.OrderItems
                    .AnyAsync(oi => oi.WholesalerProductId == id);
                
                if (isInOrder)
                {
                    TempData["Error"] = "Cannot delete product because it appears in orders.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Check if in any cart and remove from carts
                var cartItems = await _context.CartItems
                    .Where(ci => ci.WholesalerProductId == id)
                    .ToListAsync();
                
                if (cartItems.Any())
                {
                    _context.CartItems.RemoveRange(cartItems);
                }

                _context.WholesalerProducts.Remove(wholesalerProduct);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Product successfully deleted from your inventory.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool WholesalerProductExists(int id)
        {
            return _context.WholesalerProducts.Any(e => e.Id == id);
        }

        private async Task<string?> SaveImageFile(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
                return null;

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
            
            // Ensure directory exists
            Directory.CreateDirectory(uploadsFolder);
            
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Save file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            // Return relative URL
            return $"/images/products/{fileName}";
        }

        private void DeleteImageFile(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return;

            // Extract filename from URL
            if (imageUrl.StartsWith("/images/products/"))
            {
                var fileName = Path.GetFileName(imageUrl);
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products", fileName);
                
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        System.IO.File.Delete(filePath);
                    }
                    catch
                    {
                        // Ignore deletion errors
                    }
                }
            }
        }
    }
} 