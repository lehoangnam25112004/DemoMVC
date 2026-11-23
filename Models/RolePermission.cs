using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace DemoMVC.Models // Đổi theo namespace dự án của bạn
{
    public class RolePermission
    {
        public int Id { get; set; }

        [Required]
        public string RoleId { get; set; } = string.Empty;

        [ForeignKey(nameof(RoleId))]
        public IdentityRole? Role { get; set; }

        [Required]
        [StringLength(100)]
        public string ControllerName { get; set; } = string.Empty; // Ví dụ: "Student", "Major"

        [Required]
        [StringLength(100)]
        public string ActionName { get; set; } = string.Empty;     // Ví dụ: "Index", "Create", "Edit"

        public bool IsAllowed { get; set; }
    }
}