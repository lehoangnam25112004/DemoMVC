using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DemoMVC.Helpers // Đổi theo namespace dự án của bạn
{
    public static class ControllerActionScanner
    {
        public static Dictionary<string, List<string>> GetControllersAndActions()
        {
            var result = new Dictionary<string, List<string>>();
            var assembly = Assembly.GetExecutingAssembly();

            var controllers = assembly.GetTypes()
                .Where(type => typeof(Controller).IsAssignableFrom(type) && !type.IsAbstract);

            foreach (var controller in controllers)
            {
                string controllerName = controller.Name.Replace("Controller", "");

                // Bỏ qua AccountController nếu không muốn khóa login/logout/forgotpassword trong ma trận
                if (controllerName.Equals("Account", StringComparison.OrdinalIgnoreCase))
                    continue;

                var actions = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(m => !m.IsDefined(typeof(NonActionAttribute)))
                    .Where(m => typeof(IActionResult).IsAssignableFrom(m.ReturnType) ||
                                (m.ReturnType.IsGenericType && typeof(IActionResult).IsAssignableFrom(m.ReturnType.GetGenericArguments()[0])) ||
                                m.ReturnType == typeof(Task<IActionResult>) ||
                                m.ReturnType == typeof(Task))
                    .Select(m => m.Name)
                    .Distinct()
                    .OrderBy(a => a)
                    .ToList();

                if (actions.Any())
                {
                    result[controllerName] = actions;
                }
            }

            return result.OrderBy(k => k.Key).ToDictionary(k => k.Key, v => v.Value);
        }
    }
}