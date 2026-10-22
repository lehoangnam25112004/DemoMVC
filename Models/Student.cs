
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoMVC.Models
{
    public class Student
    {
        [Key]
        public int StudentId { get; set; }

        // Mã sinh viên được hệ thống tự động tạo
        [Display(Name = "Mã sinh viên")]
        [StringLength(10)]
        public string StudentCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên không được để trống")]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        [StringLength(100)]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        // Năm nhập học, ví dụ 2028
        [Required(ErrorMessage = "Năm nhập học không được để trống")]
        [Range(2000, 2099, ErrorMessage = "Năm nhập học không hợp lệ")]
        [Display(Name = "Năm nhập học")]
        public int AdmissionYear { get; set; }

        // Mã hệ đào tạo, ví dụ 21
        [Required(ErrorMessage = "Mã hệ đào tạo không được để trống")]
        [RegularExpression(@"^\d{2}$",
            ErrorMessage = "Mã hệ đào tạo phải gồm 2 chữ số")]
        [Display(Name = "Mã hệ đào tạo")]
        public string TrainingSystemCode { get; set; } = "21";

        // Mã ngành, ví dụ 05
        [Required(ErrorMessage = "Mã ngành không được để trống")]
        [RegularExpression(@"^\d{2}$",
            ErrorMessage = "Mã ngành phải gồm 2 chữ số")]
        [Display(Name = "Mã ngành")]
        public string MajorCode { get; set; } = string.Empty;

        // Khóa ngoại kết nối với bảng Major
        [Display(Name = "Khoa/Ngành")]
        public int? MajorId { get; set; }

        [ForeignKey(nameof(MajorId))]
        public Major? Major { get; set; }
    }
}


