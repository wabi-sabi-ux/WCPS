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

        // GET: /AdminClaims or /AdminClaims/Index
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

        // GET: /AdminClaims/Process/5
        [Authorize(Roles = "CpdAdmin")]
        public async Task<IActionResult> Process(int id)
        {
            var claim = await _db.ClaimRequests
                                 .Include(c => c.Employee)
                                 .FirstOrDefaultAsync(c => c.Id == id);

            if (claim == null) return NotFound();

            // Load audit trail entries for this claim
            var audits = await _db.AuditTrails
                                  .Where(a => a.Entity == "ClaimRequest" && a.EntityId == claim.Id)
                                  .OrderByDescending(a => a.Timestamp)
                                  .ToListAsync();

            // Map to view model, resolving performer names
            var auditVmList = new List<WCPS.WebApp.ViewModels.AuditEntryViewModel>();
            foreach (var a in audits)
            {
                string performedByName = "System";
                if (!string.IsNullOrEmpty(a.PerformedById))
                {
                    var user = await _db.Users.FindAsync(a.PerformedById);
                    if (user != null)
                    {
                        performedByName = !string.IsNullOrWhiteSpace(user.FullName)
                                          ? user.FullName
                                          : (user.Email ?? user.UserName ?? "Unknown");
                    }
                    else
                    {
                        performedByName = a.PerformedById;
                    }
                }

                auditVmList.Add(new WCPS.WebApp.ViewModels.AuditEntryViewModel
                {
                    Action = a.Action,
                    PerformedById = a.PerformedById,
                    PerformedByName = performedByName,
                    Timestamp = a.Timestamp
                });
            }

            var vm = new WCPS.WebApp.ViewModels.AdminProcessViewModel
            {
                ClaimId = claim.Id,
                ClaimRef = claim.ClaimRef,
                Title = claim.Title,
                AmountClaimed = claim.AmountClaimed,
                EmployeeName = claim.Employee?.FullName ?? claim.EmployeeId,
                AuditEntries = auditVmList
            };

            return View(vm);
        }



        // POST: /AdminClaims/Process
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "CpdAdmin")]
        public async Task<IActionResult> Process(WCPS.WebApp.ViewModels.AdminProcessViewModel model)
        {
            // If model binding/validation fails, collect errors and re-render the view with audits so you can see what's wrong.
            if (!ModelState.IsValid)
            {
                // collect readable modelstate errors
                var errors = ModelState.Values
                                .SelectMany(v => v.Errors)
                                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage)
                                .Where(msg => !string.IsNullOrWhiteSpace(msg))
                                .ToList();

                TempData["ProcessErrors"] = errors.Any() ? string.Join(" | ", errors) : "Unknown model binding error";

                // reload claim & audits so view can render properly with the existing model
                var claim = await _db.ClaimRequests
                                     .Include(c => c.Employee)
                                     .FirstOrDefaultAsync(c => c.Id == model.ClaimId);

                if (claim != null)
                {
                    // populate any missing display info on the model
                    model.ClaimRef = claim.ClaimRef;
                    model.Title = claim.Title;
                    model.AmountClaimed = claim.AmountClaimed;
                    model.EmployeeName = claim.Employee?.FullName ?? claim.EmployeeId;

                    var audits = await _db.AuditTrails
                                          .Where(a => a.Entity == "ClaimRequest" && a.EntityId == claim.Id)
                                          .OrderByDescending(a => a.Timestamp)
                                          .ToListAsync();

                    // map to AuditEntryViewModel if available
                    model.AuditEntries = audits.Select(a => new WCPS.WebApp.ViewModels.AuditEntryViewModel
                    {
                        Action = a.Action,
                        PerformedById = a.PerformedById,
                        PerformedByName = (a.PerformedById == null ? "System" : (_db.Users.FirstOrDefault(u => u.Id == a.PerformedById)?.FullName ?? a.PerformedById)),
                        Timestamp = a.Timestamp
                    }).ToList();
                }

                return View(model);
            }

            // Validate semantics
            if (model.Action == "approve" && (model.AmountApproved == null || model.AmountApproved <= 0))
            {
                TempData["ProcessErrors"] = "Approved amount must be provided and greater than 0 when approving.";
                // reload audits same as above for full view
                var claimReload = await _db.ClaimRequests
                                          .Include(c => c.Employee)
                                          .FirstOrDefaultAsync(c => c.Id == model.ClaimId);
                if (claimReload != null)
                {
                    model.ClaimRef = claimReload.ClaimRef;
                    model.Title = claimReload.Title;
                    model.AmountClaimed = claimReload.AmountClaimed;
                    model.EmployeeName = claimReload.Employee?.FullName ?? claimReload.EmployeeId;

                    var auditsR = await _db.AuditTrails
                                           .Where(a => a.Entity == "ClaimRequest" && a.EntityId == claimReload.Id)
                                           .OrderByDescending(a => a.Timestamp)
                                           .ToListAsync();
                    model.AuditEntries = auditsR.Select(a => new WCPS.WebApp.ViewModels.AuditEntryViewModel
                    {
                        Action = a.Action,
                        PerformedById = a.PerformedById,
                        PerformedByName = (a.PerformedById == null ? "System" : (_db.Users.FirstOrDefault(u => u.Id == a.PerformedById)?.FullName ?? a.PerformedById)),
                        Timestamp = a.Timestamp
                    }).ToList();
                }

                return View(model);
            }

            // Load the claim for update
            var claimToUpdate = await _db.ClaimRequests.FirstOrDefaultAsync(c => c.Id == model.ClaimId);
            if (claimToUpdate == null) return NotFound();

            if (model.Action == "approve")
            {
                claimToUpdate.Status = WCPS.WebApp.Models.ClaimStatus.Approved;
                claimToUpdate.AmountApproved = model.AmountApproved;
            }
            else if (model.Action == "reject")
            {
                claimToUpdate.Status = WCPS.WebApp.Models.ClaimStatus.Rejected;
                claimToUpdate.AmountApproved = null;
            }
            else
            {
                TempData["ProcessErrors"] = "Unknown action.";
                return View(model);
            }

            claimToUpdate.ProcessedAt = DateTime.UtcNow;
            var adminId = _userManager.GetUserId(User);
            claimToUpdate.ProcessedById = adminId;

            _db.ClaimRequests.Update(claimToUpdate);

            var audit = new WCPS.WebApp.Models.AuditTrail
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
