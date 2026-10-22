using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DemoMVC.Models
{
    public class Major
    {
        [Key]
        [Display(Name = "ID khoa")]
        public int MajorId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã khoa")]
        [StringLength(2, ErrorMessage = "Mã khoa không quá 2 ký tự")]
        [Display(Name = "Mã khoa")]
        public string MajorCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên khoa")]
        [StringLength(100, ErrorMessage = "Tên khoa không quá 100 ký tự")]
        [Display(Name = "Tên khoa")]
        public string MajorName { get; set; } = string.Empty;

        // Navigation Property: Một khoa chứa nhiều Student (Quan hệ 1 - Nhiều)
        public ICollection<Student>? Students { get; set; }
    }
}

