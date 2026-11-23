namespace DemoMVC.Models.ViewModels
{
    public class ActionPermissionItem
    {
        public string ControllerName { get; set; } = string.Empty;
        public string ActionName { get; set; } = string.Empty;
        public bool IsAllowed { get; set; }
    }

    public class ControllerPermissionGroup
    {
        public string ControllerName { get; set; } = string.Empty;
        public List<ActionPermissionItem> Actions { get; set; } = new();
    }

    public class RolePermissionMatrixViewModel
    {
        public string RoleId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public List<ControllerPermissionGroup> Controllers { get; set; } = new();
    }
}