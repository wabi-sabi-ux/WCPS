using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WCPS.WebApp.Data;
using WCPS.WebApp.Models;

var builder = WebApplication.CreateBuilder(args);

// ---- Services (must be before Build) ----
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// ---- Role + Admin seeding ----
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = { "Employee", "CpdAdmin", "Finance" };
    foreach (var r in roles)
    {
        if (!await roleManager.RoleExistsAsync(r))
            await roleManager.CreateAsync(new IdentityRole(r));
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
        }
        else
        {
            foreach (var e in result.Errors)
                Console.WriteLine($"Admin seed error: {e.Code} - {e.Description}");
        }
    }
}

// ---- Middleware pipeline ----
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
