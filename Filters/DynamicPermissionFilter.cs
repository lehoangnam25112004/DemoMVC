using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using DemoMVC.Data;
using DemoMVC.Models;

namespace DemoMVC.Filters
{
    public class DynamicPermissionFilter : IAsyncAuthorizationFilter
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IMemoryCache _cache;

        public DynamicPermissionFilter(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IMemoryCache cache)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _cache = cache;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Bỏ qua nếu Action hoặc Controller có [AllowAnonymous]
            var hasAllowAnonymous = context.ActionDescriptor.EndpointMetadata
                .OfType<IAllowAnonymous>()
                .Any();
            if (hasAllowAnonymous)
                return;

            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var actionName = context.RouteData.Values["action"]?.ToString();

            if (string.IsNullOrEmpty(controllerName) || string.IsNullOrEmpty(actionName))
                return;

            // Bỏ qua AccountController để không khóa luồng đăng nhập / đăng ký / quên mật khẩu
            if (controllerName.Equals("Account", StringComparison.OrdinalIgnoreCase))
                return;

            var userPrincipal = context.HttpContext.User;
            if (userPrincipal?.Identity?.IsAuthenticated != true)
            {
                context.Result = new ChallengeResult();
                return;
            }

            var user = await _userManager.GetUserAsync(userPrincipal);
            if (user == null)
            {
                context.Result = new ChallengeResult();
                return;
            }

            var userRoleNames = await _userManager.GetRolesAsync(user);
            if (!userRoleNames.Any())
            {
                context.Result = new ForbidResult();
                return;
            }

            // Role Admin mặc định toàn quyền để tránh tự khóa chính mình
            if (userRoleNames.Contains("Admin"))
                return;

            foreach (var roleName in userRoleNames)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role == null) continue;

                var cacheKey = $"RolePerms_{role.Id}";
                if (!_cache.TryGetValue(cacheKey, out List<RolePermission>? allowedList))
                {
                    allowedList = await _context.RolePermissions
                        .Where(p => p.RoleId == role.Id && p.IsAllowed)
                        .ToListAsync();

                    _cache.Set(cacheKey, allowedList, TimeSpan.FromMinutes(10));
                }

                bool isAllowed = allowedList!.Any(p =>
                    p.ControllerName.Equals(controllerName, StringComparison.OrdinalIgnoreCase) &&
                    p.ActionName.Equals(actionName, StringComparison.OrdinalIgnoreCase));

                if (isAllowed)
                    return;
            }

            context.Result = new ForbidResult();
        }
    }
}