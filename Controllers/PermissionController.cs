using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using DemoMVC.Data;
using DemoMVC.Helpers;
using DemoMVC.Models;
using DemoMVC.Models.ViewModels;

namespace DemoMVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PermissionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IMemoryCache _cache;

        public PermissionController(
            ApplicationDbContext context,
            RoleManager<IdentityRole> roleManager,
            IMemoryCache cache)
        {
            _context = context;
            _roleManager = roleManager;
            _cache = cache;
        }

        // GET: /Permission?roleId=...
        public async Task<IActionResult> Index(string? roleId)
        {
            var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            if (!roles.Any())
            {
                TempData["Error"] = "Chưa có Role nào trong hệ thống.";
                return View(new RolePermissionMatrixViewModel());
            }

            var selectedRole = string.IsNullOrEmpty(roleId)
                ? roles.First()
                : roles.FirstOrDefault(r => r.Id == roleId) ?? roles.First();

            ViewBag.Roles = roles;

            var savedPermissions = await _context.RolePermissions
                .Where(p => p.RoleId == selectedRole.Id)
                .ToListAsync();

            var scanned = ControllerActionScanner.GetControllersAndActions();

            var model = new RolePermissionMatrixViewModel
            {
                RoleId = selectedRole.Id,
                RoleName = selectedRole.Name ?? string.Empty,
                Controllers = scanned.Select(kvp => new ControllerPermissionGroup
                {
                    ControllerName = kvp.Key,
                    Actions = kvp.Value.Select(actionName =>
                    {
                        var existing = savedPermissions.FirstOrDefault(p =>
                            p.ControllerName.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase) &&
                            p.ActionName.Equals(actionName, StringComparison.OrdinalIgnoreCase));

                        return new ActionPermissionItem
                        {
                            ControllerName = kvp.Key,
                            ActionName = actionName,
                            IsAllowed = existing?.IsAllowed ?? false
                        };
                    }).ToList()
                }).ToList()
            };

            return View(model);
        }

        // POST: /Permission/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(RolePermissionMatrixViewModel model)
        {
            if (string.IsNullOrEmpty(model.RoleId))
                return NotFound();

            var role = await _roleManager.FindByIdAsync(model.RoleId);
            if (role == null)
                return NotFound();

            var desiredItems = model.Controllers
                .SelectMany(c => c.Actions)
                .Select(a => new
                {
                    ControllerName = a.ControllerName.Trim(),
                    ActionName = a.ActionName.Trim(),
                    a.IsAllowed
                })
                .ToList();

            var existingList = await _context.RolePermissions
                .Where(p => p.RoleId == model.RoleId)
                .ToListAsync();

            foreach (var item in desiredItems)
            {
                var existing = existingList.FirstOrDefault(p =>
                    p.ControllerName.Equals(item.ControllerName, StringComparison.OrdinalIgnoreCase) &&
                    p.ActionName.Equals(item.ActionName, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    existing.IsAllowed = item.IsAllowed;
                }
                else
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = model.RoleId,
                        ControllerName = item.ControllerName,
                        ActionName = item.ActionName,
                        IsAllowed = item.IsAllowed
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Xóa cache quyền của role này để áp dụng ngay lập tức
            _cache.Remove($"RolePerms_{model.RoleId}");

            TempData["Success"] = $"Đã lưu phân quyền cho Role '{role.Name}'.";
            return RedirectToAction(nameof(Index), new { roleId = model.RoleId });
        }
    }
}