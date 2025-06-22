using DFTRK.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DFTRK.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager, 
            RoleManager<IdentityRole> roleManager)
        {
            // Make sure the database is created
            context.Database.EnsureCreated();

            // Look for any users. If there are already users, the database has been seeded
            if (context.Users.Any())
            {
                return; // Database has been seeded
            }

            // Create roles
            var roles = new[] { "Admin", "Wholesaler", "Retailer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Create admin user
            var adminUser = new ApplicationUser
            {
                UserName = "admin@dftrk.com",
                Email = "admin@dftrk.com",
                EmailConfirmed = true,
                BusinessName = "DFTRK Admin",
                UserType = UserType.Admin
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            // Create demo wholesaler
            var wholesalerUser = new ApplicationUser
            {
                UserName = "wholesaler@demo.com",
                Email = "wholesaler@demo.com",
                EmailConfirmed = true,
                BusinessName = "Demo Wholesaler",
                Address = "123 Wholesale Ave",
                City = "Wholesale City",
                State = "WS",
                PostalCode = "12345",
                Country = "USA",
                TaxId = "W12345678",
                UserType = UserType.Wholesaler
            };

            result = await userManager.CreateAsync(wholesalerUser, "Wholesaler123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(wholesalerUser, "Wholesaler");
            }

            // Create demo retailer
            var retailerUser = new ApplicationUser
            {
                UserName = "retailer@demo.com",
                Email = "retailer@demo.com",
                EmailConfirmed = true,
                BusinessName = "Demo Retailer",
                Address = "456 Retail St",
                City = "Retail City",
                State = "RT",
                PostalCode = "67890",
                Country = "USA",
                TaxId = "R87654321",
                UserType = UserType.Retailer
            };

            result = await userManager.CreateAsync(retailerUser, "Retailer123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(retailerUser, "Retailer");
            }

            // We're no longer creating default categories and products
            // Users will create their own categories and products
        }
    }
} 