using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WCPS.WebApp.Data;
using WCPS.WebApp.Models;
using WCPS.WebApp.ViewModels;

namespace WCPS.WebApp.Controllers
{
    [Authorize] // only logged-in users can access
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ClaimsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _db = db;
            _userManager = userManager;
            _env = env;
        }

        // GET: /Claims
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var claims = await _db.ClaimRequests
                                 .AsNoTracking()
                                 .Where(c => c.EmployeeId == userId)
                                 .OrderByDescending(c => c.CreatedAt)
                                 .ToListAsync();
            return View(claims);
        }

        // GET: /Claims/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Claims/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateClaimViewModel model, Microsoft.AspNetCore.Http.IFormFile? Receipt)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = _userManager.GetUserId(User);

            // Handle file upload (optional)
            string? savedRelativePath = null;
            if (Receipt != null && Receipt.Length > 0)
            {
                // Validate content type and extension
                var allowedContentType = "application/pdf";
                var ext = Path.GetExtension(Receipt.FileName).ToLowerInvariant();
                if (Receipt.ContentType != allowedContentType || ext != ".pdf")
                {
                    ModelState.AddModelError("Receipt", "Only PDF files are allowed.");
                    return View(model);
                }

                // max 5 MB
                const long maxBytes = 5 * 1024 * 1024;
                if (Receipt.Length > maxBytes)
                {
                    ModelState.AddModelError("Receipt", "File too large. Max 5 MB.");
                    return View(model);
                }

                // Ensure uploads folder exists: wwwroot/uploads/{userId}
                var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads");
                var userFolder = Path.Combine(uploadsRoot, userId);
                Directory.CreateDirectory(userFolder);

                // Generate unique filename and save
                var fileName = $"{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(userFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Receipt.CopyToAsync(stream);
                }

                // Store relative path for DB like "userId/filename.pdf"
                savedRelativePath = Path.Combine(userId, fileName).Replace('\\', '/');
            }

            var claim = new ClaimRequest
            {
                EmployeeId = userId,
                Title = model.Title,
                Description = model.Description,
                AmountClaimed = model.AmountClaimed,
                Status = ClaimStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ReceiptPath = savedRelativePath
            };

            _db.ClaimRequests.Add(claim);
            await _db.SaveChangesAsync();

            // Add audit trail entry
            var audit = new AuditTrail
            {
                Entity = "ClaimRequest",
                EntityId = claim.Id,
                Action = "CREATE",
                PerformedById = userId,
                Timestamp = DateTime.UtcNow
            };
            _db.AuditTrails.Add(audit);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Claims/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);
            var claim = await _db.ClaimRequests
                                 .Include(c => c.Employee)
                                 .FirstOrDefaultAsync(c => c.Id == id);

            if (claim == null) return NotFound();

            // Only owner or admins/finance can view
            if (claim.EmployeeId != userId && !User.IsInRole("CpdAdmin") && !User.IsInRole("Finance"))
                return Forbid();

            return View(claim);
        }

        // Download / Preview receipt PDF (authorized)
        [Authorize]
        public async Task<IActionResult> DownloadReceipt(int id, string? mode = null)
        {
            var claim = await _db.ClaimRequests.FindAsync(id);
            if (claim == null || string.IsNullOrEmpty(claim.ReceiptPath))
                return NotFound();

            // authorization: owner or admins/finance can access
            var userId = _userManager.GetUserId(User);
            if (claim.EmployeeId != userId && !User.IsInRole("CpdAdmin") && !User.IsInRole("Finance"))
                return Forbid();

            var fullPath = Path.Combine(_env.WebRootPath, "uploads", claim.ReceiptPath.Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            var contentType = "application/pdf";
            // If caller explicitly asks for download, return a PhysicalFile with a download filename (forces attachment)
            if (!string.IsNullOrEmpty(mode) && mode.Equals("download", StringComparison.OrdinalIgnoreCase))
            {
                var fileName = Path.GetFileName(fullPath);
                return PhysicalFile(fullPath, contentType, fileName); // forces download
            }

            // Otherwise, return inline preview: stream the file without a download filename (browser can render inline)
            var fs = System.IO.File.OpenRead(fullPath);
            return new FileStreamResult(fs, contentType);
        }

    }
}
