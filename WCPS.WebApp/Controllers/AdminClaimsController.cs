using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WCPS.WebApp.Data;
using WCPS.WebApp.Models;
using WCPS.WebApp.ViewModels;

namespace WCPS.WebApp.Controllers
{

    [Authorize(Roles = "CpdAdmin")]
    public class AdminClaimsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminClaimsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        //GET: /AdminClaims
        public async Task<IActionResult> Index()
        {
            var pending = await _db.ClaimRequests
                                   .AsNoTracking()
                                   .Include(c => c.Employee)
                                   .Where(c => c.Status == ClaimStatus.Pending)
                                   .OrderBy(c => c.CreatedAt)
                                   .ToListAsync();

            return View(pending);
        }
        //GET: /AdminClaims/Audit
        [Authorize(Roles = "CpdAdmin")]
        public async Task<IActionResult> Audit()
        {
            var audits = await _db.AuditTrails
                                  .AsNoTracking()
                                  .OrderByDescending(a => a.Timestamp)
                                  .Take(200) 
                                  .ToListAsync();

            // Map to simple vm
            var vm = audits.Select(a => new WCPS.WebApp.ViewModels.AuditEntryViewModel
            {
                Action = a.Action,
                PerformedById = a.PerformedById,
                PerformedByName = string.IsNullOrWhiteSpace(a.PerformedById) ? "System"
                                   : (_db.Users.FirstOrDefault(u => u.Id == a.PerformedById)?.FullName ?? a.PerformedById),
                Timestamp = a.Timestamp
            }).ToList();

            return View(vm);
        }

        // GET: /AdminClaims/Process/{id}
        public async Task<IActionResult> Process(int id)
        {
            var claim = await _db.ClaimRequests
                                 .Include(c => c.Employee)
                                 .FirstOrDefaultAsync(c => c.Id == id);

            if (claim == null) return NotFound();
            //Loads audit
            var audits = await _db.AuditTrails
                                  .Where(a => a.Entity == "ClaimRequest" && a.EntityId == claim.Id)
                                  .OrderByDescending(a => a.Timestamp)
                                  .ToListAsync();

            var vm = new AdminProcessViewModel
            {
                ClaimId = claim.Id,
                ClaimRef = claim.ClaimRef,
                Title = claim.Title,
                AmountClaimed = claim.AmountClaimed,
                EmployeeName = claim.Employee?.FullName ?? claim.EmployeeId,
                AuditEntries = audits.Select(a => new AuditEntryViewModel
                {
                    Action = a.Action,
                    PerformedById = a.PerformedById,
                    PerformedByName = (a.PerformedById == null ? "System" :
                        (_db.Users.FirstOrDefault(u => u.Id == a.PerformedById)?.FullName ?? a.PerformedById)),
                    Timestamp = a.Timestamp
                }).ToList()
            };

            return View(vm);
        }

        // POST: /AdminClaims/Process
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(AdminProcessViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ProcessErrors"] = "Invalid input. Please correct errors and try again.";
                return RedirectToAction("Process", new { id = model.ClaimId });
            }

            var claimToUpdate = await _db.ClaimRequests.FirstOrDefaultAsync(c => c.Id == model.ClaimId);
            if (claimToUpdate == null) return NotFound();

            var adminId = _userManager.GetUserId(User);

            if (model.Action == "approve")
            {
                if (!model.AmountApproved.HasValue || model.AmountApproved <= 0)
                {
                    TempData["ProcessErrors"] = "Approved amount must be greater than 0.";
                    return RedirectToAction("Process", new { id = model.ClaimId });
                }

                claimToUpdate.Status = ClaimStatus.Approved;
                claimToUpdate.AmountApproved = model.AmountApproved;
            }
            else if (model.Action == "reject")
            {
                claimToUpdate.Status = ClaimStatus.Rejected;
                claimToUpdate.AmountApproved = null;
            }
            else
            {
                TempData["ProcessErrors"] = "Unknown action.";
                return RedirectToAction("Process", new { id = model.ClaimId });
            }

            claimToUpdate.ProcessedAt = DateTime.UtcNow;
            claimToUpdate.ProcessedById = adminId;

            _db.ClaimRequests.Update(claimToUpdate);

            var audit = new AuditTrail
            {
                Entity = "ClaimRequest",
                EntityId = claimToUpdate.Id,
                Action = model.Action.ToUpperInvariant(),
                PerformedById = adminId,
                Timestamp = DateTime.UtcNow
            };
            _db.AuditTrails.Add(audit);

            await _db.SaveChangesAsync();

            TempData["Notice"] = $"Claim {claimToUpdate.ClaimRef} {model.Action}d successfully.";
            return RedirectToAction("Index");
        }
    }
}
