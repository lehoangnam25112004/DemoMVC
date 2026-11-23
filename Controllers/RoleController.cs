using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DemoMVC.Models;

namespace DemoMVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RoleController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public RoleController(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        // GET: /Role
        public async Task<IActionResult> Index()
        {
            var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            return View(roles);
        }

        // GET: /Role/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Role/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                ModelState.AddModelError("", "Tên Role không được để trống.");
                return View();
            }

            roleName = roleName.Trim();

            if (await _roleManager.RoleExistsAsync(roleName))
            {
                ModelState.AddModelError("", $"Role '{roleName}' đã tồn tại.");
                return View();
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
            if (result.Succeeded)
            {
                TempData["Success"] = $"Đã tạo Role '{roleName}' thành công.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View();
        }

        // GET: /Role/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            return View(role);
        }

        // POST: /Role/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, string newRoleName)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrWhiteSpace(newRoleName))
            {
                ModelState.AddModelError("", "Tên Role không hợp lệ.");
                var currentRole = await _roleManager.FindByIdAsync(id);
                return View(currentRole);
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            newRoleName = newRoleName.Trim();

            if (await _roleManager.RoleExistsAsync(newRoleName) &&
                !string.Equals(role.Name, newRoleName, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("", $"Role '{newRoleName}' đã tồn tại.");
                return View(role);
            }

            role.Name = newRoleName;
            role.NormalizedName = newRoleName.ToUpperInvariant();

            var result = await _roleManager.UpdateAsync(role);
            if (result.Succeeded)
            {
                TempData["Success"] = "Cập nhật Role thành công.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(role);
        }

        // POST: /Role/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            if (string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Không thể xóa Role mặc định 'Admin'.";
                return RedirectToAction(nameof(Index));
            }

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            if (usersInRole.Any())
            {
                TempData["Error"] = $"Không thể xóa Role '{role.Name}' vì đang có {usersInRole.Count} người dùng thuộc Role này.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Đã xóa Role '{role.Name}'.";
            }
            else
            {
                TempData["Error"] = "Xóa Role thất bại.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}