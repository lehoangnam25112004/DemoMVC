using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DemoMVC.Models;
using DemoMVC.Models.ViewModels;

namespace DemoMVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserRoleController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserRoleController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: /UserRole
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.Email ?? u.UserName)
                .ToListAsync();

            var model = new List<UserWithRolesViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                model.Add(new UserWithRolesViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Roles = roles
                });
            }

            return View(model);
        }

        // GET: /UserRole/Manage/{userId}
        public async Task<IActionResult> Manage(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return NotFound();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var allRoles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();

            var userRoles = await _userManager.GetRolesAsync(user);

            var viewModel = new ManageUserRolesViewModel
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Roles = allRoles.Select(r => new RoleSelectionItem
                {
                    RoleId = r.Id,
                    RoleName = r.Name ?? string.Empty,
                    IsSelected = !string.IsNullOrEmpty(r.Name) && userRoles.Contains(r.Name)
                }).ToList()
            };

            return View(viewModel);
        }

        // POST: /UserRole/Manage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(ManageUserRolesViewModel model)
        {
            if (string.IsNullOrEmpty(model.UserId)) return NotFound();

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);

            var selectedRoleNames = model.Roles
                .Where(r => r.IsSelected && !string.IsNullOrWhiteSpace(r.RoleName))
                .Select(r => r.RoleName)
                .Distinct()
                .ToList();

            var rolesToAdd = selectedRoleNames.Except(currentRoles).ToList();
            var rolesToRemove = currentRoles.Except(selectedRoleNames).ToList();

            if (rolesToAdd.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    ModelState.AddModelError("", "Không thể gán thêm một số Role cho người dùng.");
                    return View(model);
                }
            }

            if (rolesToRemove.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    ModelState.AddModelError("", "Không thể gỡ một số Role khỏi người dùng.");
                    return View(model);
                }
            }

            // Làm mới security stamp để cập nhật trạng thái quyền nhanh hơn
            await _userManager.UpdateSecurityStampAsync(user);

            TempData["Success"] = $"Đã cập nhật Role cho tài khoản '{user.Email ?? user.UserName}'.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /UserRole/EditProfile/{id}
        public async Task<IActionResult> EditProfile(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(new EditUserProfileViewModel
            {
                Id = user.Id,
                FullName = user.FullName ?? string.Empty,
                Email = user.Email ?? string.Empty
            });
        }

        // POST: /UserRole/EditProfile/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(string id, EditUserProfileViewModel model)
        {
            if (id != model.Id) return BadRequest();

            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            var existingEmailUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingEmailUser != null && existingEmailUser.Id != user.Id)
            {
                ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng bởi tài khoản khác.");
                return View(model);
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                if (!passResult.Succeeded)
                {
                    foreach (var error in passResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
            }

            await _userManager.UpdateSecurityStampAsync(user);

            TempData["Success"] = $"Đã cập nhật thông tin tài khoản '{user.Email ?? user.UserName}'.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /UserRole/DeleteUser/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["Error"] = "Không thể tự xóa tài khoản đang đăng nhập.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Đã xóa tài khoản '{user.Email ?? user.UserName}'.";
            }
            else
            {
                TempData["Error"] = "Xóa tài khoản thất bại.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}