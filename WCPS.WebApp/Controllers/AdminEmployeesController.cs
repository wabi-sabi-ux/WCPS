using System;
using System.Collections.Generic;
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
    public class AdminEmployeesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminEmployeesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // GET: /AdminEmployees
        public async Task<IActionResult> Index()
        {
            var users = await _db.Users.AsNoTracking().ToListAsync();

            var vmList = new List<AdminEmployeeViewModel>(users.Count);
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                vmList.Add(new AdminEmployeeViewModel
                {
                    Id = u.Id,
                    EmployeeNo = u.EmployeeNo,
                    FullName = u.FullName,
                    Email = u.Email ?? u.UserName ?? "",
                    Roles = string.Join(", ", roles),
                    LastLoginAt = u.LastLoginAt,
                    BankAccountNumber = u.BankAccountNumber
                });
            }

            return View(vmList);
        }

        // GET: /AdminEmployees/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var vm = new AdminEmployeeViewModel
            {
                Id = user.Id,
                EmployeeNo = user.EmployeeNo,
                FullName = user.FullName,
                Email = user.Email ?? user.UserName ?? "",
                Roles = string.Join(", ", roles),
                LastLoginAt = user.LastLoginAt,
                BankAccountNumber = user.BankAccountNumber
            };

            return View(vm);
        }

        // GET: /AdminEmployees/Create
        public IActionResult Create()
        {
            var vm = new AdminEmployeeViewModel();
            return View(vm);
        }

        // POST: /AdminEmployees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminEmployeeViewModel model, string? initialPassword)
        {
            if (!ModelState.IsValid) return View(model);

            var tempPassword = string.IsNullOrWhiteSpace(initialPassword) ? "ChangeMe@123" : initialPassword;

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmployeeNo = model.EmployeeNo,
                FullName = model.FullName,
                BankAccountNumber = model.BankAccountNumber,
                LastLoginAt = null
            };

            var createResult = await _userManager.CreateAsync(user, tempPassword);
            if (!createResult.Succeeded)
            {
                foreach (var e in createResult.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }

            // assign roles if provided (comma separated)
            if (!string.IsNullOrWhiteSpace(model.Roles))
            {
                var roles = model.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var role in roles)
                {
                    try
                    {
                        await _userManager.AddToRoleAsync(user, role);
                    }
                    catch
                    {
                        // ignore or log
                    }
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /AdminEmployees/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var vm = new AdminEmployeeViewModel
            {
                Id = user.Id,
                EmployeeNo = user.EmployeeNo,
                FullName = user.FullName,
                Email = user.Email ?? user.UserName ?? "",
                Roles = string.Join(", ", roles),
                LastLoginAt = user.LastLoginAt,
                BankAccountNumber = user.BankAccountNumber
            };

            return View(vm);
        }

        // POST: /AdminEmployees/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, AdminEmployeeViewModel model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            user.EmployeeNo = model.EmployeeNo;
            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email; // keep username in sync with email
            user.BankAccountNumber = model.BankAccountNumber;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var e in updateResult.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }

            // update roles — remove all existing and add new ones (simple approach)
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            if (!string.IsNullOrWhiteSpace(model.Roles))
            {
                var roles = model.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var role in roles)
                {
                    try
                    {
                        await _userManager.AddToRoleAsync(user, role);
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /AdminEmployees/Delete/{id}
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var vm = new AdminEmployeeViewModel
            {
                Id = user.Id,
                EmployeeNo = user.EmployeeNo,
                FullName = user.FullName,
                Email = user.Email ?? user.UserName ?? "",
                Roles = string.Join(", ", roles),
                LastLoginAt = user.LastLoginAt,
                BankAccountNumber = user.BankAccountNumber
            };

            return View(vm);
        }

        // POST: /AdminEmployees/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                var vm = new AdminEmployeeViewModel
                {
                    Id = user.Id,
                    EmployeeNo = user.EmployeeNo,
                    FullName = user.FullName,
                    Email = user.Email ?? user.UserName ?? "",
                    Roles = string.Join(", ", await _userManager.GetRolesAsync(user)),
                    LastLoginAt = user.LastLoginAt,
                    BankAccountNumber = user.BankAccountNumber
                };
                foreach (var e in deleteResult.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View("Delete", vm);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
