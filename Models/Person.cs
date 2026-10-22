using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoMVC.Models
{
    public class Person
    {
        [Key]

        [Required(ErrorMessage = "Tên không được để trống")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        [StringLength(100)]
        public string Address { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email không được để trống")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        // --- BỔ SUNG KHÓA NGOẠI KẾT NỐI SANG DEPARTMENT ---
        [Display(Name = "Phòng ban")]
        public int? DepartmentId { get; set; } // Cho phép null nếu nhân sự chưa có phòng ban

        [ForeignKey("DepartmentId")]
        public Department? Department { get; set; } // Navigation Property

    }
}



