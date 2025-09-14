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
                var ext = Path.GetExtension(Receipt.FileName).ToLowerInvariant();

                // --- Security: validate extension & content type ---
                if (ext != ".pdf" || Receipt.ContentType != "application/pdf")
                {
                    ModelState.AddModelError("Receipt", "Only PDF files are allowed.");
                    return View(model);
                }

                // --- Limit size ---
                const long maxBytes = 5 * 1024 * 1024;
                if (Receipt.Length > maxBytes)
                {
                    ModelState.AddModelError("Receipt", "File too large. Max 5 MB.");
                    return View(model);
                }

                // --- Save file ---
                var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads");
                var userFolder = Path.Combine(uploadsRoot, userId);
                Directory.CreateDirectory(userFolder);

                var fileName = $"{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(userFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Receipt.CopyToAsync(stream);
                }

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

            // --- Audit: record claim creation ---
            _db.AuditTrails.Add(new AuditTrail
            {
                Entity = "ClaimRequest",
                EntityId = claim.Id,
                Action = "CREATE",
                PerformedById = userId,
                Timestamp = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            TempData["Notice"] = "Claim submitted successfully.";
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

        // GET: /Claims/DownloadReceipt/{id}
        [Authorize]
        public async Task<IActionResult> DownloadReceipt(int id, string? mode = null)
        {
            var claim = await _db.ClaimRequests.FindAsync(id);
            if (claim == null || string.IsNullOrEmpty(claim.ReceiptPath))
                return NotFound();

            var userId = _userManager.GetUserId(User);

            // --- Authorization check ---
            if (claim.EmployeeId != userId && !User.IsInRole("CpdAdmin") && !User.IsInRole("Finance"))
                return Forbid();

            var fullPath = Path.Combine(_env.WebRootPath, "uploads", claim.ReceiptPath.Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            const string contentType = "application/pdf";
            var fileName = Path.GetFileName(fullPath);

            // --- Audit: record receipt access ---
            _db.AuditTrails.Add(new AuditTrail
            {
                Entity = "ClaimRequest",
                EntityId = claim.Id,
                Action = string.IsNullOrEmpty(mode) ? "RECEIPT_PREVIEW" : "RECEIPT_DOWNLOAD",
                PerformedById = userId,
                Timestamp = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            if (!string.IsNullOrEmpty(mode) && mode.Equals("download", StringComparison.OrdinalIgnoreCase))
            {
                return PhysicalFile(fullPath, contentType, fileName); // download
            }

            var fs = System.IO.File.OpenRead(fullPath);
            return new FileStreamResult(fs, contentType); // inline preview
        }
    }
}
