using DFTRK.Data;
using DFTRK.Models;
using DFTRK.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System;

namespace DFTRK.Controllers
{
    [Authorize]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Products
        public async Task<IActionResult> Index(string searchString, int? categoryId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (User.IsInRole("Admin"))
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .ToListAsync();
                return View("AdminIndex", products);
            }
            else if (User.IsInRole("Wholesaler"))
            {
                var wholesalerProducts = await _context.WholesalerProducts
                    .Include(wp => wp.Product)
                    .ThenInclude(p => p.Category)
                    .Where(wp => wp.WholesalerId == user.Id)
                    .ToListAsync();
                return View("WholesalerIndex", wholesalerProducts);
            }
            else if (User.IsInRole("Retailer"))
            {
                // Start with base query
                var query = _context.WholesalerProducts
                    .Include(wp => wp.Product)
                    .ThenInclude(p => p.Category)
                    .Include(wp => wp.Wholesaler)
                    .Where(wp => wp.IsActive && wp.StockQuantity > 0);
                    
                // Apply search filter if provided
                if (!string.IsNullOrEmpty(searchString))
                {
                    searchString = searchString.ToLower();
                    query = query.Where(wp => 
                        wp.Product.Name.ToLower().Contains(searchString) || 
                        wp.Product.Description.ToLower().Contains(searchString) ||
                        wp.Product.SKU.ToLower().Contains(searchString) ||
                        wp.Wholesaler.BusinessName.ToLower().Contains(searchString)
                    );
                }
                
                // Apply category filter if provided
                if (categoryId.HasValue)
                {
                    query = query.Where(wp => wp.Product.CategoryId == categoryId.Value);
                }
                
                // Get all categories for the filter dropdown
                var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                ViewBag.Categories = categories;
                
                // Execute query and return results
                var availableProducts = await query.ToListAsync();
                return View("RetailerIndex", availableProducts);
            }

            return RedirectToAction("AccessDenied", "Account");
        }

        // GET: Products/Details/5
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

            if (User.IsInRole("Admin"))
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(m => m.Id == id);
                
                if (product == null)
                {
                    return NotFound();
                }

                return View("AdminDetails", product);
            }
            else if (User.IsInRole("Wholesaler"))
            {
                var wholesalerProduct = await _context.WholesalerProducts
                    .Include(wp => wp.Product)
                    .ThenInclude(p => p.Category)
                    .FirstOrDefaultAsync(wp => wp.Id == id && wp.WholesalerId == user.Id);
                
                if (wholesalerProduct == null)
                {
                    return NotFound();
                }

                return View("WholesalerDetails", wholesalerProduct);
            }
            else if (User.IsInRole("Retailer"))
            {
                var wholesalerProduct = await _context.WholesalerProducts
                    .Include(wp => wp.Product)
                    .ThenInclude(p => p.Category)
                    .Include(wp => wp.Wholesaler)
                    .FirstOrDefaultAsync(wp => wp.Id == id && wp.IsActive);
                
                if (wholesalerProduct == null)
                {
                    return NotFound();
                }

                // Check if this product is already in the retailer's cart
                var cart = await GetOrCreateCartAsync(user.Id);
                var isInCart = await _context.CartItems
                    .AnyAsync(ci => ci.CartId == cart.Id && ci.WholesalerProductId == id);

                var viewModel = new RetailerProductDetailsViewModel
                {
                    WholesalerProduct = wholesalerProduct,
                    IsInCart = isInCart,
                    Quantity = 1 // Default quantity
                };

                return View("RetailerDetails", viewModel);
            }

            return RedirectToAction("AccessDenied", "Account");
        }

        // GET: Products/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewData["CategoryId"] = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(ProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                string? imageUrl = null;
                
                // Handle image upload
                if (model.ImageFile != null)
                {
                    imageUrl = await SaveImageFile(model.ImageFile);
                }

                var product = new Product
                {
                    Name = model.Name,
                    Description = model.Description,
                    CategoryId = model.CategoryId,
                    ImageUrl = imageUrl
                };

                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            
            ViewData["CategoryId"] = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", model.CategoryId);
            return View(model);
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

        // GET: Products/CreateWholesalerProduct
        [Authorize(Roles = "Wholesaler,Admin")]
        public async Task<IActionResult> CreateWholesalerProduct()
        {
            ViewData["ProductId"] = new SelectList(await _context.Products.ToListAsync(), "Id", "Name");
            return View();
        }

        // POST: Products/CreateWholesalerProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Wholesaler,Admin")]
        public async Task<IActionResult> CreateWholesalerProduct(WholesalerProductViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Check if this wholesaler already offers this product
                var exists = await _context.WholesalerProducts
                    .AnyAsync(wp => wp.ProductId == model.ProductId && wp.WholesalerId == user.Id);
                
                if (exists)
                {
                    ModelState.AddModelError(string.Empty, "You already offer this product. Please edit the existing entry instead.");
                    ViewData["ProductId"] = new SelectList(await _context.Products.ToListAsync(), "Id", "Name", model.ProductId);
                    return View(model);
                }

                var wholesalerProduct = new WholesalerProduct
                {
                    ProductId = model.ProductId,
                    WholesalerId = user.Id,
                    Price = model.Price,
                    StockQuantity = model.StockQuantity,
                    IsActive = model.IsActive
                };

                _context.Add(wholesalerProduct);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            
            ViewData["ProductId"] = new SelectList(await _context.Products.ToListAsync(), "Id", "Name", model.ProductId);
            return View(model);
        }

        // GET: Products/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            
            var model = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                CategoryId = product.CategoryId,
                ImageUrl = product.ImageUrl
            };
            
            ViewData["CategoryId"] = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
            return View(model);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, ProductViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var product = await _context.Products.FindAsync(id);
                    if (product == null)
                    {
                        return NotFound();
                    }

                    product.Name = model.Name;
                    product.Description = model.Description;
                    product.CategoryId = model.CategoryId;
                    
                    // Handle image upload
                    if (model.ImageFile != null)
                    {
                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(product.ImageUrl))
                        {
                            DeleteImageFile(product.ImageUrl);
                        }
                        
                        // Save new image
                        product.ImageUrl = await SaveImageFile(model.ImageFile);
                    }
                    else
                    {
                        // Keep existing image URL if no new file uploaded
                        product.ImageUrl = model.ImageUrl;
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(model.Id))
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
            
            ViewData["CategoryId"] = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", model.CategoryId);
            return View(model);
        }

        // GET: Products/EditWholesalerProduct/5
        [Authorize(Roles = "Wholesaler,Admin")]
        public async Task<IActionResult> EditWholesalerProduct(int? id)
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

            var query = _context.WholesalerProducts
                .Include(wp => wp.Product)
                .AsQueryable();
                
            // If not admin, only allow editing own products
            if (!User.IsInRole("Admin"))
            {
                query = query.Where(wp => wp.WholesalerId == user.Id);
            }
            
            var wholesalerProduct = await query.FirstOrDefaultAsync(wp => wp.Id == id);
            
            if (wholesalerProduct == null)
            {
                return NotFound();
            }
            
            var model = new WholesalerProductViewModel
            {
                Id = wholesalerProduct.Id,
                ProductId = wholesalerProduct.ProductId,
                Price = wholesalerProduct.Price,
                StockQuantity = wholesalerProduct.StockQuantity,
                IsActive = wholesalerProduct.IsActive
            };
            
            ViewData["ProductName"] = wholesalerProduct.Product?.Name;
            return View(model);
        }

        // POST: Products/EditWholesalerProduct/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Wholesaler,Admin")]
        public async Task<IActionResult> EditWholesalerProduct(int id, WholesalerProductViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var query = _context.WholesalerProducts.AsQueryable();
                    
                    // If not admin, only allow editing own products
                    if (!User.IsInRole("Admin"))
                    {
                        query = query.Where(wp => wp.WholesalerId == user.Id);
                    }
                    
                    var wholesalerProduct = await query.FirstOrDefaultAsync(wp => wp.Id == id);
                    
                    if (wholesalerProduct == null)
                    {
                        return NotFound();
                    }

                    wholesalerProduct.Price = model.Price;
                    wholesalerProduct.StockQuantity = model.StockQuantity;
                    wholesalerProduct.IsActive = model.IsActive;

                    _context.Update(wholesalerProduct);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WholesalerProductExists(model.Id))
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
            
            var product = await _context.Products.FindAsync(model.ProductId);
            ViewData["ProductName"] = product?.Name;
            return View(model);
        }

        // POST: Products/AddToCart/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Retailer")]
        public async Task<IActionResult> AddToCart(int id, int quantity)
        {
            if (quantity <= 0)
            {
                return BadRequest("Quantity must be greater than zero");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var wholesalerProduct = await _context.WholesalerProducts
                .FirstOrDefaultAsync(wp => wp.Id == id && wp.IsActive && wp.StockQuantity >= quantity);
            
            if (wholesalerProduct == null)
            {
                return NotFound("Product not found or insufficient stock");
            }

            // Get or create cart
            var cart = await GetOrCreateCartAsync(user.Id);

            // Check if item is already in cart
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.WholesalerProductId == id);

            if (cartItem != null)
            {
                // Update existing cart item
                cartItem.Quantity += quantity;
                cartItem.Price = wholesalerProduct.Price;
                _context.Update(cartItem);
            }
            else
            {
                // Add new cart item
                cartItem = new CartItem
                {
                    CartId = cart.Id,
                    WholesalerProductId = id,
                    Quantity = quantity,
                    Price = wholesalerProduct.Price
                };
                _context.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Cart");
        }

        // GET: Products/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                // Check if product is in use by any wholesaler
                var isInUse = await _context.WholesalerProducts
                    .AnyAsync(wp => wp.ProductId == id);
                
                if (isInUse)
                {
                    ModelState.AddModelError(string.Empty, "Cannot delete product because it is offered by wholesalers.");
                    return View(product);
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        // GET: Products/DeleteWholesalerProduct/5
        [Authorize(Roles = "Wholesaler,Admin")]
        public async Task<IActionResult> DeleteWholesalerProduct(int? id)
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

            var query = _context.WholesalerProducts
                .Include(wp => wp.Product)
                .AsQueryable();
                
            // If not admin, only allow managing own products
            if (!User.IsInRole("Admin"))
            {
                query = query.Where(wp => wp.WholesalerId == user.Id);
            }
            
            var wholesalerProduct = await query.FirstOrDefaultAsync(wp => wp.Id == id);
            
            if (wholesalerProduct == null)
            {
                return NotFound();
            }

            return View(wholesalerProduct);
        }

        // POST: Products/DeleteWholesalerProduct/5
        [HttpPost, ActionName("DeleteWholesalerProduct")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Wholesaler,Admin")]
        public async Task<IActionResult> DeleteWholesalerProductConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var query = _context.WholesalerProducts.AsQueryable();
                
            // If not admin, only allow managing own products
            if (!User.IsInRole("Admin"))
            {
                query = query.Where(wp => wp.WholesalerId == user.Id);
            }
            
            var wholesalerProduct = await query.FirstOrDefaultAsync(wp => wp.Id == id);
            
            if (wholesalerProduct != null)
            {
                // Check if the product is in any orders
                var isInOrder = await _context.OrderItems
                    .AnyAsync(oi => oi.WholesalerProductId == id);
                
                if (isInOrder)
                {
                    ModelState.AddModelError(string.Empty, "Cannot delete product because it appears in orders.");
                    return View(wholesalerProduct);
                }

                // Check if in any cart
                var isInCart = await _context.CartItems
                    .AnyAsync(ci => ci.WholesalerProductId == id);
                
                if (isInCart)
                {
                    // Just remove from all carts
                    var cartItems = await _context.CartItems
                        .Where(ci => ci.WholesalerProductId == id)
                        .ToListAsync();
                    
                    _context.CartItems.RemoveRange(cartItems);
                }

                _context.WholesalerProducts.Remove(wholesalerProduct);
                await _context.SaveChangesAsync();
            }
            
            // Redirect to WholesalerProducts controller, not Products controller
            return RedirectToAction("Index", "WholesalerProducts");
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        private bool WholesalerProductExists(int id)
        {
            return _context.WholesalerProducts.Any(e => e.Id == id);
        }

        private async Task<Cart> GetOrCreateCartAsync(string retailerId)
        {
            var cart = await _context.Carts
                .FirstOrDefaultAsync(c => c.RetailerId == retailerId);
            
            if (cart == null)
            {
                cart = new Cart
                {
                    RetailerId = retailerId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        // Helper method to check if user is authorized to manage a wholesaler product
        private bool CanManageWholesalerProduct(string userId, string wholesalerId)
        {
            return User.IsInRole("Admin") || userId == wholesalerId;
        }
    }
} 