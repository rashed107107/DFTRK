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
    public class RetailerProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RetailerProductsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: RetailerProducts
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            try
            {
                // Get all retailer products
                var retailerProducts = await _context.RetailerProducts
                    .Include(rp => rp.WholesalerProduct)
                    .ThenInclude(wp => wp.Product)
                    .ThenInclude(p => p.Category)
                    .Include(rp => rp.WholesalerProduct)
                    .ThenInclude(wp => wp.Wholesaler)
                    .Where(rp => rp.RetailerId == user.Id)
                    .ToListAsync();

                var groupedProducts = new List<RetailerProductViewModel>();

                // Handle regular wholesaler products (with WholesalerProductId)
                var wholesalerProducts = retailerProducts.Where(rp => rp.WholesalerProductId.HasValue);
                var groupedWholesalerProducts = wholesalerProducts
                    .GroupBy(rp => rp.WholesalerProductId)
                    .Select(group => new RetailerProductViewModel
                    {
                        Id = group.First().Id,
                        WholesalerProduct = group.First().WholesalerProduct,
                        PurchasePrice = group.First().PurchasePrice,
                        SellingPrice = group.First().SellingPrice,
                        StockQuantity = group.Sum(rp => rp.StockQuantity),
                        Notes = string.Join(", ", group.Where(rp => !string.IsNullOrEmpty(rp.Notes))
                                                     .Select(rp => rp.Notes)
                                                     .Distinct())
                    });
                groupedProducts.AddRange(groupedWholesalerProducts);

                // Handle partnership products (without WholesalerProductId)
                var partnershipProducts = retailerProducts.Where(rp => !rp.WholesalerProductId.HasValue);
                var groupedPartnershipProducts = new List<RetailerProductViewModel>();
                
                foreach (var rp in partnershipProducts)
                {
                    // Extract PartnerProductId from Notes to get full partnership information
                    var partnerProductId = ExtractPartnerProductIdFromNotes(rp.Notes);
                    RetailerPartnerProduct? partnerProduct = null;
                    
                    if (partnerProductId.HasValue)
                    {
                        partnerProduct = await _context.RetailerPartnerProducts
                            .Include(pp => pp.Category)
                            .Include(pp => pp.Partnership)
                            .FirstOrDefaultAsync(pp => pp.Id == partnerProductId.Value);
                    }
                    
                    groupedPartnershipProducts.Add(new RetailerProductViewModel
                    {
                        Id = rp.Id,
                        WholesalerProduct = null, // No wholesaler product for partnerships
                        PurchasePrice = rp.PurchasePrice,
                        SellingPrice = rp.SellingPrice,
                        StockQuantity = rp.StockQuantity,
                        Notes = rp.Notes,
                        PartnershipProductName = ExtractProductNameFromNotes(rp.Notes),
                        PartnershipCategoryName = partnerProduct?.Category?.Name,
                        PartnershipSupplierName = partnerProduct?.Partnership?.WholesalerName ?? partnerProduct?.Partnership?.PartnershipName
                    });
                }
                
                groupedProducts.AddRange(groupedPartnershipProducts);

                return View(groupedProducts);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error in RetailerProducts Index: {ex.Message}");
                // Return empty list in case of error
                return View(new List<RetailerProductViewModel>());
            }
        }

        private string ExtractProductNameFromNotes(string notes)
        {
            if (string.IsNullOrEmpty(notes))
                return "Unknown Partnership Product";

            // Extract product name from notes like "Partnership product: ProductName - ..."
            var prefix = "Partnership product: ";
            if (notes.StartsWith(prefix))
            {
                var start = prefix.Length;
                var end = notes.IndexOf(" - ", start);
                if (end > start)
                {
                    return notes.Substring(start, end - start);
                }
                else
                {
                    // If no " - " found, take everything after the prefix
                    return notes.Substring(start);
                }
            }

            return notes; // Return full notes if format doesn't match
        }

        private int? ExtractPartnerProductIdFromNotes(string notes)
        {
            if (string.IsNullOrEmpty(notes))
                return null;

            // Look for pattern like "Added from order #39" or "Last updated from order #39"
            var orderPattern = "order #";
            var orderIndex = notes.LastIndexOf(orderPattern);
            if (orderIndex == -1)
                return null;

            var orderStart = orderIndex + orderPattern.Length;
            var orderEnd = orderStart;
            while (orderEnd < notes.Length && char.IsDigit(notes[orderEnd]))
            {
                orderEnd++;
            }

            if (orderEnd > orderStart && int.TryParse(notes.Substring(orderStart, orderEnd - orderStart), out var orderId))
            {
                // Find the OrderItem that created this product
                var orderItem = _context.OrderItems
                    .FirstOrDefault(oi => oi.OrderId == orderId && 
                                         oi.PartnerProductId.HasValue && 
                                         !string.IsNullOrEmpty(oi.ProductName) &&
                                         notes.Contains(oi.ProductName));
                
                return orderItem?.PartnerProductId;
            }

            return null;
        }

        // GET: RetailerProducts/Details/5
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

            try
            {
                // Get the specific product
                var retailerProduct = await _context.RetailerProducts
                    .Include(rp => rp.WholesalerProduct)
                    .ThenInclude(wp => wp.Product)
                    .ThenInclude(p => p.Category)
                    .Include(rp => rp.WholesalerProduct)
                    .ThenInclude(wp => wp.Wholesaler)
                    .FirstOrDefaultAsync(m => m.Id == id && m.RetailerId == user.Id);
                
                if (retailerProduct == null)
                {
                    return NotFound();
                }

                // Check if the navigation properties are valid
                if (retailerProduct.WholesalerProduct == null || retailerProduct.WholesalerProduct.Product == null)
                {
                    TempData["Error"] = "This product has missing or invalid data. It may have been deleted by the wholesaler.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if there are other entries for the same product
                var totalQuantity = await _context.RetailerProducts
                    .Where(rp => rp.RetailerId == user.Id && 
                           rp.WholesalerProductId == retailerProduct.WholesalerProductId)
                    .SumAsync(rp => rp.StockQuantity);

                // If there are multiple entries, update the view with the total quantity
                if (totalQuantity != retailerProduct.StockQuantity)
                {
                    ViewBag.TotalQuantity = totalQuantity;
                    ViewBag.HasMultipleEntries = true;
                }

                return View(retailerProduct);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while retrieving the product details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: RetailerProducts/Edit/5
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

            try
            {
                var retailerProduct = await _context.RetailerProducts
                    .Include(rp => rp.WholesalerProduct)
                    .ThenInclude(wp => wp.Product)
                    .FirstOrDefaultAsync(rp => rp.Id == id && rp.RetailerId == user.Id);
                
                if (retailerProduct == null)
                {
                    return NotFound();
                }
                
                // Check if the navigation properties are valid
                if (retailerProduct.WholesalerProduct == null || retailerProduct.WholesalerProduct.Product == null)
                {
                    TempData["Error"] = "This product has missing or invalid data. It may have been deleted by the wholesaler.";
                    return RedirectToAction(nameof(Index));
                }
                
                var model = new RetailerProductEditViewModel
                {
                    Id = retailerProduct.Id,
                    ProductName = retailerProduct.WholesalerProduct.Product.Name,
                    PurchasePrice = retailerProduct.PurchasePrice,
                    SellingPrice = retailerProduct.SellingPrice,
                    StockQuantity = retailerProduct.StockQuantity
                };
                
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while retrieving the product for editing.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: RetailerProducts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RetailerProductEditViewModel model)
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
                    // Get the specific product being edited
                    var retailerProduct = await _context.RetailerProducts
                        .FirstOrDefaultAsync(rp => rp.Id == id && rp.RetailerId == user.Id);
                    
                    if (retailerProduct == null)
                    {
                        return NotFound();
                    }

                    // Store original values for logging
                    var originalSellingPrice = retailerProduct.SellingPrice;
                    var originalStockQuantity = retailerProduct.StockQuantity;

                    // Update with new values directly (not incrementing)
                    retailerProduct.SellingPrice = model.SellingPrice;
                    retailerProduct.StockQuantity = model.StockQuantity;

                    // Log the changes
                    Console.WriteLine($"Product ID: {id}, Original Stock: {originalStockQuantity}, New Stock: {model.StockQuantity}");

                    _context.Update(retailerProduct);
                    await _context.SaveChangesAsync();
                    
                    TempData["Success"] = "Product updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RetailerProductExists(model.Id))
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
            return View(model);
        }

        // GET: RetailerProducts/MigrateFromOrders
        [HttpGet]
        public async Task<IActionResult> MigrateFromOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            try
            {
                // Get all completed and delivered orders
                var orders = await _context.Orders
                    .Include(o => o.Items)
                    .Where(o => o.RetailerId == user.Id && 
                          (o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered))
                    .ToListAsync();

                int migratedProducts = 0;
                int updatedProducts = 0;
                int skippedProducts = 0;

                // Process each order
                foreach (var order in orders)
                {
                    foreach (var orderItem in order.Items)
                    {
                        // Get the actual WholesalerProduct with all its navigation properties
                        var wholesalerProduct = await _context.WholesalerProducts
                            .Include(wp => wp.Product)
                            .Include(wp => wp.Wholesaler)
                            .FirstOrDefaultAsync(wp => wp.Id == orderItem.WholesalerProductId);

                        // Skip items with missing WholesalerProduct
                        if (wholesalerProduct == null || wholesalerProduct.Product == null)
                        {
                            skippedProducts++;
                            continue;
                        }

                        // Check if product already exists in retailer inventory
                        var existingProduct = await _context.RetailerProducts
                            .FirstOrDefaultAsync(rp => rp.RetailerId == user.Id && 
                                                rp.WholesalerProductId == orderItem.WholesalerProductId);

                        if (existingProduct != null)
                        {
                            // Update existing product (only if we're not double-counting)
                            var alreadyCounted = existingProduct.Notes?.Contains($"Migrated from Order #{order.Id}") == true;
                                
                            if (!alreadyCounted)
                            {
                                existingProduct.StockQuantity += orderItem.Quantity;
                                existingProduct.Notes = string.IsNullOrEmpty(existingProduct.Notes)
                                    ? $"Updated from Order #{order.Id}"
                                    : $"{existingProduct.Notes}, Updated from Order #{order.Id}";
                                _context.Update(existingProduct);
                                updatedProducts++;
                            }
                        }
                        else
                        {
                            // Add new product to retailer inventory
                            var retailerProduct = new RetailerProduct
                            {
                                RetailerId = user.Id,
                                WholesalerProductId = orderItem.WholesalerProductId, // Now nullable, can be null for partnership orders
                                PurchasePrice = orderItem.UnitPrice,
                                SellingPrice = orderItem.UnitPrice * 1.3m, // Default 30% markup
                                StockQuantity = orderItem.Quantity,
                                Notes = $"Migrated from Order #{order.Id}"
                            };
                            
                            _context.RetailerProducts.Add(retailerProduct);
                            migratedProducts++;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                if (migratedProducts == 0 && updatedProducts == 0)
                {
                    if (skippedProducts > 0)
                    {
                        TempData["Warning"] = $"No products were imported. {skippedProducts} products were skipped because they no longer exist.";
                    }
                    else
                    {
                        TempData["Info"] = "No new products were found to import from your orders.";
                    }
                }
                else
                {
                    var skippedMessage = skippedProducts > 0 ? $" ({skippedProducts} products were skipped because they no longer exist)" : "";
                    TempData["Success"] = $"Successfully migrated {migratedProducts} new products and updated {updatedProducts} existing products from your previous orders.{skippedMessage}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while importing products from orders.";
            }
            
            return RedirectToAction(nameof(Index));
        }

        // GET: RetailerProducts/ConsolidateInventory
        [HttpGet]
        public async Task<IActionResult> ConsolidateInventory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            try
            {
                // Get all retailer products
                var retailerProducts = await _context.RetailerProducts
                    .Where(rp => rp.RetailerId == user.Id)
                    .ToListAsync();

                // Group by WholesalerProductId to find duplicates
                var duplicateGroups = retailerProducts
                    .GroupBy(rp => rp.WholesalerProductId)
                    .Where(g => g.Count() > 1)
                    .ToList();

                int consolidatedCount = 0;

                // Process each group of duplicates
                foreach (var group in duplicateGroups)
                {
                    var products = group.ToList();
                    
                    // Select the first product as the primary one to keep
                    var primaryProduct = products.First();
                    
                    // Sum up quantities and collect notes from all duplicates
                    var totalQuantity = products.Sum(p => p.StockQuantity);
                    var allNotes = string.Join(", ", products
                        .Where(p => !string.IsNullOrEmpty(p.Notes))
                        .Select(p => p.Notes)
                        .Distinct());
                    
                    // Update the primary product
                    primaryProduct.StockQuantity = totalQuantity;
                    primaryProduct.Notes = !string.IsNullOrEmpty(allNotes) 
                        ? $"{allNotes}, Consolidated on {DateTime.Now:yyyy-MM-dd}" 
                        : $"Consolidated on {DateTime.Now:yyyy-MM-dd}";
                    
                    _context.Update(primaryProduct);
                    
                    // Remove all duplicates except the primary one
                    foreach (var duplicate in products.Skip(1))
                    {
                        _context.RetailerProducts.Remove(duplicate);
                    }
                    
                    consolidatedCount += products.Count - 1;
                }

                await _context.SaveChangesAsync();

                if (consolidatedCount > 0)
                {
                    TempData["Success"] = $"Successfully consolidated {consolidatedCount} duplicate product entries.";
                }
                else
                {
                    TempData["Info"] = "No duplicate products found to consolidate.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while consolidating inventory.";
                Console.WriteLine($"Error in ConsolidateInventory: {ex.Message}");
            }
            
            return RedirectToAction(nameof(Index));
        }

        // GET: RetailerProducts/Delete/5
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

            var retailerProduct = await _context.RetailerProducts
                .Include(rp => rp.WholesalerProduct)
                    .ThenInclude(wp => wp.Product)
                        .ThenInclude(p => p.Category)
                .Include(rp => rp.WholesalerProduct)
                    .ThenInclude(wp => wp.Wholesaler)
                .FirstOrDefaultAsync(rp => rp.Id == id && rp.RetailerId == user.Id);

            if (retailerProduct == null)
            {
                return NotFound();
            }

            return View(retailerProduct);
        }

        // POST: RetailerProducts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var retailerProduct = await _context.RetailerProducts
                .FirstOrDefaultAsync(rp => rp.Id == id && rp.RetailerId == user.Id);

            if (retailerProduct != null)
            {
                _context.RetailerProducts.Remove(retailerProduct);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Product successfully removed from your inventory.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool RetailerProductExists(int id)
        {
            return _context.RetailerProducts.Any(e => e.Id == id);
        }
    }
} 