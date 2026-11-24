using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DemoMVC.Models.ViewModels
{
    public enum ExcelImportMode
    {
        Append = 1,    // Ghi bổ sung
        Overwrite = 2  // Ghi đè
    }

    public class ExcelImportPreviewRow
    {
        public int RowNumber { get; set; }
        public bool IsValid { get; set; }
        public bool IsDuplicate { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public Dictionary<string, string> Values { get; set; } = new();
    }

    public class ExcelImportViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn bảng dữ liệu.")]
        [Display(Name = "Bảng dữ liệu")]
        public string SelectedTable { get; set; } = "Student";

        [Required(ErrorMessage = "Vui lòng chọn chế độ nhập.")]
        [Display(Name = "Chế độ nhập dữ liệu")]
        public ExcelImportMode ImportMode { get; set; } = ExcelImportMode.Append;

        [Display(Name = "File Excel (.xlsx)")]
        public IFormFile? ExcelFile { get; set; }

        // Dùng để giữ dữ liệu sau bước Preview sang bước Xác nhận
        public string? StagedPayloadJson { get; set; }
        public string? UploadedFileName { get; set; }

        public List<ExcelImportPreviewRow> PreviewRows { get; set; } = new();
        public int TotalRows => PreviewRows.Count;
        public int ValidRowsCount => PreviewRows.Count(r => r.IsValid);
        public int InvalidRowsCount => PreviewRows.Count(r => !r.IsValid);
        public int DuplicateRowsCount => PreviewRows.Count(r => r.IsValid && r.IsDuplicate);

        public Dictionary<string, string> AvailableTables { get; set; } = new()
        {
            { "Student", "Sinh viên (Students)" },
            { "Major", "Ngành đào tạo (Majors)" }
        };
    }
}