using Microsoft.AspNetCore.Identity;
using WCPS.WebApp.Models;

namespace WCPS.WebApp.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(ApplicationDbContext db,RoleManager<IdentityRole> roleManager,UserManager<ApplicationUser> userManager,ILogger logger)
        {
            string[] roles = { "Employee", "CpdAdmin", "Finance" };

            foreach (var r in roles)
            {
                if (!await roleManager.RoleExistsAsync(r))
                {
                    await roleManager.CreateAsync(new IdentityRole(r));
                    Console.WriteLine($"Role seeded: {r}");
                }
            }   

            var adminEmail = "admin@wcps.local";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "CPD Admin",
                    EmployeeNo = "ADMIN001",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@1234");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "CpdAdmin");
                    Console.WriteLine("Admin user seeded successfully.");
                }
                else
                {
                    foreach (var e in result.Errors)
                        Console.WriteLine($"Admin seed error: {e.Code} - {e.Description}");
                }
            }
        }
    }
}
