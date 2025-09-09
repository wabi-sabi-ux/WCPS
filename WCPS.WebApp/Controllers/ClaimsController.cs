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
    [Authorize] // only logged-in users can access
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClaimsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
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
        public async Task<IActionResult> Create(CreateClaimViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = _userManager.GetUserId(User);

            var claim = new ClaimRequest
            {
                EmployeeId = userId,
                Title = model.Title,
                Description = model.Description,
                AmountClaimed = model.AmountClaimed,
                Status = ClaimStatus.Pending,
                CreatedAt = DateTime.UtcNow
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
    }
}
