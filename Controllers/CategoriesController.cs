using DFTRK.Data;
using DFTRK.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace DFTRK.Controllers
{
    [Authorize(Roles = "Admin,Wholesaler")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CategoriesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            IQueryable<Category> query = _context.Categories.Include(c => c.CreatedBy);
            
            // If wholesaler, only show their own categories
            if (User.IsInRole("Wholesaler"))
            {
                query = query.Where(c => c.CreatedById == user.Id);
            }

            var categories = await query.OrderBy(c => c.Name).ToListAsync();
            return View(categories);
        }

        // GET: Categories/Details/5
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

            var category = await _context.Categories.FirstOrDefaultAsync(m => m.Id == id);
            
            if (category == null)
            {
                return NotFound();
            }

            // Check if wholesaler is authorized to view this category
            if (User.IsInRole("Wholesaler") && category.CreatedById != user.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(category);
        }

        // GET: Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Category category)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Associate category with creator
            category.CreatedById = user.Id;

            if (ModelState.IsValid)
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Categories/Edit/5
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

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            // Check if wholesaler is authorized to edit this category
            if (User.IsInRole("Wholesaler") && category.CreatedById != user.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(category);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Get the original category to check ownership
            var originalCategory = await _context.Categories.FindAsync(id);
            if (originalCategory == null)
            {
                return NotFound();
            }

            // Check if wholesaler is authorized to edit this category
            if (User.IsInRole("Wholesaler") && originalCategory.CreatedById != user.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Preserve the creator ID
            category.CreatedById = originalCategory.CreatedById;

            if (ModelState.IsValid)
            {
                try
                {
                    // Detach the original entity to avoid tracking conflicts
                    _context.Entry(originalCategory).State = EntityState.Detached;
                    
                    // Update with the new entity
                    _context.Entry(category).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
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
            return View(category);
        }

        // GET: Categories/Delete/5
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

            var category = await _context.Categories.FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            // Check if wholesaler is authorized to delete this category
            if (User.IsInRole("Wholesaler") && category.CreatedById != user.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            // Check if wholesaler is authorized to delete this category
            if (User.IsInRole("Wholesaler") && category.CreatedById != user.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Check if category is in use
            bool inUse = await _context.Products.AnyAsync(p => p.CategoryId == id);
            if (inUse)
            {
                ModelState.AddModelError("", "Cannot delete category that is in use by products.");
                return View(category);
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
} 