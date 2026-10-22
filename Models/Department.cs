using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DemoMVC.Models
{
    public class Department
    {
        [Key]
        [Display(Name = "Mã phòng ban")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên phòng ban")]
        [StringLength(100, ErrorMessage = "Tên phòng ban không quá 100 ký tự")]
        [Display(Name = "Tên phòng ban")]
        public string DepartmentName { get; set; } = string.Empty;

        // Navigation Property: Một phòng ban chứa nhiều Person (Quan hệ 1 - Nhiều)
        public ICollection<Person>? Persons { get; set; }
    }
}