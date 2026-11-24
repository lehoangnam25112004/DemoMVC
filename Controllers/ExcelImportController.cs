using DemoMVC.Models.ViewModels;
using DemoMVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DemoMVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ExcelImportController : Controller
    {
        private readonly ExcelImportService _importService;

        public ExcelImportController(ExcelImportService importService)
        {
            _importService = importService;
        }

        // GET: /ExcelImport
        public IActionResult Index(string? table = "Student")
        {
            var vm = new ExcelImportViewModel
            {
                SelectedTable = _importService.IsAllowedTable(table ?? string.Empty) ? table! : "Student"
            };
            return View(vm);
        }

        // GET: /ExcelImport/DownloadTemplate?table=Student
        public IActionResult DownloadTemplate(string table = "Student")
        {
            if (!_importService.IsAllowedTable(table))
                return BadRequest("Bảng dữ liệu không hợp lệ.");

            var fileBytes = _importService.GenerateTemplateWorkbook(table);
            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Template_{table}.xlsx");
        }

        // POST: /ExcelImport/Preview
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Preview(ExcelImportViewModel model)
        {
            if (!Enum.IsDefined(model.ImportMode))
                ModelState.AddModelError(nameof(model.ImportMode), "Chế độ nhập dữ liệu không hợp lệ.");

            if (!ModelState.IsValid)
                return View("Index", model);

            if (!_importService.IsAllowedTable(model.SelectedTable))
            {
                ModelState.AddModelError(nameof(model.SelectedTable), "Bảng dữ liệu không hợp lệ hoặc thuộc bảng Identity.");
                return View("Index", model);
            }

            if (model.ExcelFile == null || model.ExcelFile.Length == 0)
            {
                ModelState.AddModelError(nameof(model.ExcelFile), "Vui lòng chọn file Excel (.xlsx).");
                return View("Index", model);
            }

            var ext = Path.GetExtension(model.ExcelFile.FileName);
            if (!ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.ExcelFile), "Chỉ hỗ trợ định dạng file .xlsx.");
                return View("Index", model);
            }

            await using var stream = model.ExcelFile.OpenReadStream();
            var previewVm = await _importService.BuildPreviewAsync(
                stream,
                model.ExcelFile.FileName,
                model.SelectedTable,
                model.ImportMode);

            // Hidden inputs must use the newly staged values, not the submitted ModelState.
            ModelState.Clear();
            return View("Index", previewVm);
        }

        // POST: /ExcelImport/ConfirmImport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmImport(ExcelImportViewModel model)
        {
            if (!ModelState.IsValid || !Enum.IsDefined(model.ImportMode))
            {
                TempData["Error"] = "Dữ liệu xác nhận không hợp lệ. Vui lòng tải file lên và xem trước lại.";
                return RedirectToAction(nameof(Index), new { table = model.SelectedTable });
            }

            if (!_importService.IsAllowedTable(model.SelectedTable))
            {
                TempData["Error"] = "Bảng dữ liệu không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _importService.ExecuteConfirmedImportAsync(
                model.SelectedTable,
                model.ImportMode,
                model.StagedPayloadJson);

            if (result.Success)
            {
                TempData["Success"] = result.Message;
                TempData["ImportedTable"] = model.SelectedTable;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction(nameof(Index), new { table = model.SelectedTable });
        }
    }
}