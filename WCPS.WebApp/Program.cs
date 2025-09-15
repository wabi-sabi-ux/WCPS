using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WCPS.WebApp.Data;
using WCPS.WebApp.Models;
using WCPS.WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// ---- Services (must be before Build) ----
var configuration = builder.Configuration;

// Db connection (reads from appsettings / user-secrets / env)
var conn = configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(conn))
{
    throw new InvalidOperationException(
        "Missing connection string 'DefaultConnection'. " +
        "Set it using `dotnet user-secrets` or an environment variable. " +
        "See README for exact commands.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(conn));

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

// Register FileService (assumes you have this class under Services)
builder.Services.AddTransient<FileService>();

var app = builder.Build();

// ---- Role + Admin seeding (safe: logs errors instead of throwing) ----
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Run seed but don't allow seeding failures to crash the app
        await SeedData.InitializeAsync(db, roleManager, userManager, logger);
    }
    catch (Exception ex)
    {
        // Log unexpected errors, do not rethrow
        var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
        var log = loggerFactory.CreateLogger<Program>();
        log.LogError(ex, "Unexpected error during application seed. Continuing without seeding.");
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
