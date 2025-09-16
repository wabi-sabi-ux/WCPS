using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WCPS.WebApp.Data;
using WCPS.WebApp.Models;
using WCPS.WebApp.ViewModels;

namespace WCPS.WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new DashboardViewModel();

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);

                // Employee-specific stats
                vm.MyTotalClaims = await _db.ClaimRequests.CountAsync(c => c.EmployeeId == userId);
                vm.MyPendingClaims = await _db.ClaimRequests.CountAsync(c => c.EmployeeId == userId && c.Status == ClaimStatus.Pending);
                vm.MyApprovedClaims = await _db.ClaimRequests.CountAsync(c => c.EmployeeId == userId && c.Status == ClaimStatus.Approved);
                vm.MyRejectedClaims = await _db.ClaimRequests.CountAsync(c => c.EmployeeId == userId && c.Status == ClaimStatus.Rejected);

                vm.RecentMyClaims = await _db.ClaimRequests
                    .AsNoTracking()
                    .Where(c => c.EmployeeId == userId)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(6)
                    .Select(c => new ClaimSummaryVm
                    {
                        Id = c.Id,
                        ClaimRef = c.ClaimRef,
                        Title = c.Title,
                        AmountClaimed = c.AmountClaimed,
                        AmountApproved = c.AmountApproved,
                        Status = c.Status,
                        CreatedAt = c.CreatedAt
                    }).ToListAsync();
            }

            // Admin-specific stats (only compute if user is admin to save DB work)
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("CpdAdmin"))
            {
                vm.IsAdmin = true;
                vm.PendingClaimsCount = await _db.ClaimRequests.CountAsync(c => c.Status == ClaimStatus.Pending);
                vm.AdminApprovedCount = await _db.ClaimRequests.CountAsync(c => c.Status == ClaimStatus.Approved);
                vm.AdminRejectedCount = await _db.ClaimRequests.CountAsync(c => c.Status == ClaimStatus.Rejected);
                vm.TotalEmployees = await _db.Users.CountAsync();

                vm.RecentActivity = await _db.AuditTrails
                    .AsNoTracking()
                    .OrderByDescending(a => a.Timestamp)
                    .Take(10)
                    .Select(a => new AuditSummaryVm
                    {
                        Action = a.Action,
                        Entity = a.Entity,
                        EntityId = a.EntityId,
                        PerformedById = a.PerformedById,
                        Timestamp = a.Timestamp
                    }).ToListAsync();
            }

            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
