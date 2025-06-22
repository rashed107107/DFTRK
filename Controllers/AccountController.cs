using DFTRK.Models;
using DFTRK.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DFTRK.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public IActionResult Register(string userType = "")
        {
            var model = new RegisterViewModel();
            if (!string.IsNullOrEmpty(userType) && (userType.ToLower() == "wholesaler" || userType.ToLower() == "retailer"))
            {
                model.UserType = userType.ToLower() == "wholesaler" ? "Wholesaler" : "Retailer";
                ViewBag.UserTypeTitle = model.UserType;
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Validate UserType before proceeding
            if (string.IsNullOrEmpty(model.UserType) || 
                (!model.UserType.Equals("Wholesaler", StringComparison.OrdinalIgnoreCase) && 
                 !model.UserType.Equals("Retailer", StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("UserType", "Please select a valid user type.");
                ViewBag.UserTypeTitle = model.UserType;
                return View(model);
            }

            if (ModelState.IsValid)
            {
                UserType userTypeEnum;
                if (!Enum.TryParse<UserType>(model.UserType, true, out userTypeEnum))
                {
                    ModelState.AddModelError("UserType", "Invalid user type selected.");
                    ViewBag.UserTypeTitle = model.UserType;
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    BusinessName = model.BusinessName,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address,
                    City = model.City,
                    State = model.State,
                    PostalCode = "", // Set empty since we removed it
                    Country = model.Country,
                    TaxId = "", // Set empty since we removed it
                    UserType = userTypeEnum
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Add user to appropriate role
                    string roleName = model.UserType;
                    await _userManager.AddToRoleAsync(user, roleName);

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Preserve the UserType for the view
            ViewBag.UserTypeTitle = model.UserType;
            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditProfileViewModel
            {
                Email = user.Email ?? string.Empty,
                BusinessName = user.BusinessName ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address ?? string.Empty,
                City = user.City ?? string.Empty,
                State = user.State ?? string.Empty,
                Country = user.Country ?? string.Empty,
                UserType = user.UserType.ToString()
            };

            return View(model);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditProfileViewModel
            {
                Email = user.Email ?? string.Empty,
                BusinessName = user.BusinessName ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address ?? string.Empty,
                City = user.City ?? string.Empty,
                State = user.State ?? string.Empty,
                Country = user.Country ?? string.Empty,
                UserType = user.UserType.ToString()
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }

                // Update user properties
                user.Email = model.Email;
                user.UserName = model.Email;
                user.BusinessName = model.BusinessName;
                user.PhoneNumber = model.PhoneNumber;
                user.Address = model.Address;
                user.City = model.City;
                user.State = model.State;
                user.Country = model.Country;
                // Note: PostalCode and TaxId are not updated since they're not in the form

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    return RedirectToAction(nameof(Profile));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }

                var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (result.Succeeded)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    TempData["SuccessMessage"] = "Password changed successfully!";
                    return RedirectToAction(nameof(Profile));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
} 