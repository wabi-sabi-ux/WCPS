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
using WCPS.WebApp.Services;

namespace WCPS.WebApp.Controllers
{
    [Authorize] // only logged-in users can access
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly FileService _fileService;

        public ClaimsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env, FileService fileService)
        {
            _db = db;
            _userManager = userManager;
            _env = env;
            _fileService = fileService;
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

            string? savedRelativePath = null;
            if (Receipt != null && Receipt.Length > 0)
            {
                var result = await _fileService.SaveReceiptAsync(Receipt, userId);
                if (!result.Success)
                {
                    ModelState.AddModelError("Receipt", result.Error ?? "Invalid file.");
                    return View(model);
                }
                savedRelativePath = result.RelativePath;
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

        // GET: /Claims/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            var claim = await _db.ClaimRequests.FirstOrDefaultAsync(c => c.Id == id);
            if (claim == null) return NotFound();

            // Only owner can edit, and only while Pending
            if (claim.EmployeeId != userId)
                return Forbid();
            if (claim.Status != ClaimStatus.Pending)
                return BadRequest("Only pending claims can be edited.");

            var vm = new EditClaimViewModel
            {
                Id = claim.Id,
                Title = claim.Title,
                Description = claim.Description,
                AmountClaimed = claim.AmountClaimed,
                ExistingReceiptPath = claim.ReceiptPath
            };

            return View(vm);
        }

        // POST: /Claims/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditClaimViewModel model, Microsoft.AspNetCore.Http.IFormFile? Receipt)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = _userManager.GetUserId(User);
            var claim = await _db.ClaimRequests.FirstOrDefaultAsync(c => c.Id == model.Id);
            if (claim == null) return NotFound();

            if (claim.EmployeeId != userId) //omly user can edit and only while it is Pending
                return Forbid();
            if (claim.Status != ClaimStatus.Pending)
                return BadRequest("Only pending claims can be edited.");

            if (Receipt != null && Receipt.Length > 0)
            {
                var result = await _fileService.SaveReceiptAsync(Receipt, userId);
                if (!result.Success)
                {
                    ModelState.AddModelError("Receipt", result.Error ?? "Invalid file.");
                    return View(model);
                }

                // delete old file if exists
                if (!string.IsNullOrEmpty(claim.ReceiptPath))
                {
                    try
                    {
                        var oldFull = _fileService.GetFullPath(claim.ReceiptPath);
                        if (System.IO.File.Exists(oldFull))
                            System.IO.File.Delete(oldFull);
                    }
                    catch
                    {
                        // ignore deletion failure.
                    }
                }

                claim.ReceiptPath = result.RelativePath;
            }

            claim.Title = model.Title;
            claim.Description = model.Description;
            claim.AmountClaimed = model.AmountClaimed;

            _db.ClaimRequests.Update(claim);

            // audit update
            _db.AuditTrails.Add(new AuditTrail
            {
                Entity = "ClaimRequest",
                EntityId = claim.Id,
                Action = "UPDATE",
                PerformedById = userId,
                Timestamp = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            TempData["Notice"] = "Claim updated successfully.";
            return RedirectToAction(nameof(Details), new { id = claim.Id });
        }

        // GET: /Claims/Delete/{id}
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var claim = await _db.ClaimRequests.Include(c => c.Employee).FirstOrDefaultAsync(c => c.Id == id);
            if (claim == null) return NotFound();

            var isOwner = claim.EmployeeId == userId; //owner can delete their pending claim.
            var isAdmin = User.IsInRole("CpdAdmin") || User.IsInRole("Finance");

            if (!isOwner && !isAdmin) return Forbid();
            if (isOwner && claim.Status != ClaimStatus.Pending) return BadRequest("Only pending claims can be deleted by the owner.");

            return View(claim);
        }

        // POST: /Claims/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var claim = await _db.ClaimRequests.FirstOrDefaultAsync(c => c.Id == id);
            if (claim == null) return NotFound();

            var isOwner = claim.EmployeeId == userId;
            var isAdmin = User.IsInRole("CpdAdmin") || User.IsInRole("Finance");
            if (!isOwner && !isAdmin) return Forbid();
            if (isOwner && claim.Status != ClaimStatus.Pending) return BadRequest("Only pending claims can be deleted by the owner.");

            // delete file if exists
            if (!string.IsNullOrEmpty(claim.ReceiptPath))
            {
                try
                {
                    var full = _fileService.GetFullPath(claim.ReceiptPath);
                    if (System.IO.File.Exists(full))
                        System.IO.File.Delete(full);
                }
                catch
                {
                    // ignore deletion err
                }
            }

            _db.ClaimRequests.Remove(claim);

            // audit
            _db.AuditTrails.Add(new AuditTrail
            {
                Entity = "ClaimRequest",
                EntityId = claim.Id,
                Action = "DELETE",
                PerformedById = userId,
                Timestamp = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            TempData["Notice"] = "Claim deleted successfully.";
            return RedirectToAction(nameof(Index));
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

            var fullPath = _fileService.GetFullPath(claim.ReceiptPath);
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            var contentType = _fileService.GetContentType(fullPath);
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
                return PhysicalFile(fullPath, contentType, fileName);
            }

            // Inline preview (PDF/image)
            return PhysicalFile(fullPath, contentType);
        }
    }
}
