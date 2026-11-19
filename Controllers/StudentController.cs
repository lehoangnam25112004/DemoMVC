using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DemoMVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DemoMVC.Data;
using DemoMVC.Models;

namespace DemoMVC.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly StudentCodeGenerator _codeGenerator;

        public StudentController(ApplicationDbContext context, StudentCodeGenerator codeGenerator)
        {
            _context = context;
            _codeGenerator = codeGenerator;
        }

        private async Task<StudentIndexViewModel> BuildStudentIndexModel(
            string? searchTerm,
            int pageNumber,
            int pageSize,
            string sortBy,
            string sortDirection)
        {
            var validPageSizes = new[] { 10, 20, 50 };
            if (!validPageSizes.Contains(pageSize))
            {
                pageSize = 10;
            }

            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            var query = _context.Students
                .AsNoTracking()
                .Include(s => s.Major)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                query = query.Where(s =>
                    s.StudentCode.Contains(searchTerm) ||
                    s.FullName.Contains(searchTerm) ||
                    s.Email.Contains(searchTerm) ||
                    s.Address.Contains(searchTerm) ||
                    (s.Major != null && s.Major.MajorName.Contains(searchTerm)));
            }

            switch (sortBy)
            {
                case "FullName":
                    query = sortDirection == "desc"
                        ? query.OrderByDescending(s => s.FullName)
                        : query.OrderBy(s => s.FullName);
                    break;
                case "AdmissionYear":
                    query = sortDirection == "desc"
                        ? query.OrderByDescending(s => s.AdmissionYear)
                        : query.OrderBy(s => s.AdmissionYear);
                    break;
                case "MajorName":
                    query = sortDirection == "desc"
                        ? query.OrderByDescending(s => s.Major != null ? s.Major.MajorName : string.Empty)
                        : query.OrderBy(s => s.Major != null ? s.Major.MajorName : string.Empty);
                    break;
                case "StudentCode":
                    query = sortDirection == "desc"
                        ? query.OrderByDescending(s => s.StudentCode)
                        : query.OrderBy(s => s.StudentCode);
                    break;
                default:
                    query = sortDirection == "desc"
                        ? query.OrderByDescending(s => s.StudentCode)
                        : query.OrderBy(s => s.StudentCode);
                    break;
            }

            var totalItems = await query.CountAsync();
            var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling((double)totalItems / pageSize);
            if (pageNumber > totalPages)
            {
                pageNumber = totalPages;
            }

            var students = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new StudentIndexViewModel
            {
                Items = students,
                SearchTerm = searchTerm,
                SortBy = sortBy,
                SortDirection = sortDirection,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems
            };
        }

        // GET: Student
        public async Task<IActionResult> Index(
            string? searchTerm,
            int pageNumber = 1,
            int pageSize = 10,
            string sortBy = "StudentCode",
            string sortDirection = "asc")
        {
            ViewData["MajorId"] = new SelectList(_context.Majors.OrderBy(m => m.MajorCode), "MajorId", "MajorName");
            var model = await BuildStudentIndexModel(searchTerm, pageNumber, pageSize, sortBy, sortDirection);
            return View(model);
        }

        public async Task<IActionResult> ListPartial(
            string? searchTerm,
            int pageNumber = 1,
            int pageSize = 10,
            string sortBy = "StudentCode",
            string sortDirection = "asc")
        {
            var model = await BuildStudentIndexModel(searchTerm, pageNumber, pageSize, sortBy, sortDirection);
            return PartialView("_StudentTable", model);
        }

        // GET: Student/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.Major)
                .FirstOrDefaultAsync(m => m.StudentId == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // GET: Student/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["MajorId"] = new SelectList(_context.Majors.OrderBy(m => m.MajorCode), "MajorId", "MajorName");
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAjax(
            [Bind("FullName,Address,Email,AdmissionYear,TrainingSystemCode,MajorId")]
            Student student)
        {
            ModelState.Remove(nameof(Student.StudentCode));
            ModelState.Remove(nameof(Student.MajorCode));

            var major = student.MajorId.HasValue
                ? await _context.Majors.FirstOrDefaultAsync(m => m.MajorId == student.MajorId.Value)
                : null;

            if (major == null)
            {
                ModelState.AddModelError(nameof(Student.MajorId), "Vui lòng chọn một khoa hợp lệ.");
            }

            if (!ModelState.IsValid || major == null)
            {
                var errors = ModelState
                    .Where(entry => entry.Value != null && entry.Value.Errors.Count > 0)
                    .SelectMany(entry => entry.Value!.Errors.Select(
                        error => $"{entry.Key}: {error.ErrorMessage}"))
                    .ToList();

                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ.", errors });
            }

            student.MajorCode = major.MajorCode;
            student.StudentCode = await _codeGenerator.GenerateAsync(
                student.AdmissionYear,
                student.TrainingSystemCode,
                student.MajorCode
            );

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Thêm sinh viên thành công." });
        }

        // POST: Student/Create
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("FullName,Address,Email,AdmissionYear,TrainingSystemCode,MajorId")]
            Student student)
        {
            ModelState.Remove(nameof(Student.StudentCode));
            ModelState.Remove(nameof(Student.MajorCode));

            Major? major = null;

            if (student.MajorId.HasValue)
            {
                major = await _context.Majors
                    .FirstOrDefaultAsync(m => m.MajorId == student.MajorId.Value);
            }

            if (major == null)
            {
                ModelState.AddModelError(
                    nameof(Student.MajorId),
                    "Vui lòng chọn một khoa hợp lệ."
                );
            }

            if (ModelState.IsValid && major != null)
            {
                student.MajorCode = major.MajorCode;

                student.StudentCode = await _codeGenerator.GenerateAsync(
                    student.AdmissionYear,
                    student.TrainingSystemCode,
                    student.MajorCode
                );

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewData["MajorId"] = new SelectList(
                _context.Majors.OrderBy(m => m.MajorCode),
                "MajorId",
                "MajorName",
                student.MajorId
            );

            return View(student);
        }

        // GET: Student/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            ViewData["MajorId"] = new SelectList(_context.Majors, "MajorId", "MajorName", student.MajorId);
            return View(student);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAjax(int id, Student student)
        {
            if (id != student.StudentId)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(Student.StudentCode));
            ModelState.Remove(nameof(Student.MajorCode));

            var existingStudent = await _context.Students
                .Include(s => s.Major)
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (existingStudent == null)
            {
                return NotFound();
            }

            var major = student.MajorId.HasValue
                ? await _context.Majors.FirstOrDefaultAsync(m => m.MajorId == student.MajorId.Value)
                : null;

            if (major == null)
            {
                ModelState.AddModelError(nameof(Student.MajorId), "Vui lòng chọn một khoa hợp lệ.");
            }

            if (!ModelState.IsValid || major == null)
            {
                var errors = ModelState
                    .Where(m => m.Value != null && m.Value.Errors.Count > 0)
                    .SelectMany(m => m.Value!.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new { success = false, message = "Dữ liệu chỉnh sửa không hợp lệ.", errors });
            }

            existingStudent.FullName = student.FullName;
            existingStudent.Address = student.Address;
            existingStudent.Email = student.Email;
            existingStudent.AdmissionYear = student.AdmissionYear;
            existingStudent.TrainingSystemCode = student.TrainingSystemCode;
            existingStudent.MajorId = student.MajorId;
            existingStudent.MajorCode = major.MajorCode;
            existingStudent.StudentCode = existingStudent.StudentCode;

            try
            {
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cập nhật sinh viên thành công." });
            }
            catch (DbUpdateConcurrencyException)
            {
                return BadRequest(new { success = false, message = "Bản ghi này đã bị thay đổi bởi người khác." });
            }
        }

        // POST: Student/Edit/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StudentId,FullName,Address,Email,AdmissionYear,TrainingSystemCode,MajorId")] Student student)
        {
            if (id != student.StudentId)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(Student.StudentCode));
            ModelState.Remove(nameof(Student.MajorCode));

            var existingStudent = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (existingStudent == null)
            {
                return NotFound();
            }

            var major = student.MajorId.HasValue
                ? await _context.Majors.FirstOrDefaultAsync(m => m.MajorId == student.MajorId.Value)
                : null;

            if (major == null)
            {
                ModelState.AddModelError(nameof(Student.MajorId), "Vui lòng chọn một khoa hợp lệ.");
            }

            if (ModelState.IsValid && major != null)
            {
                try
                {
                    existingStudent.FullName = student.FullName;
                    existingStudent.Address = student.Address;
                    existingStudent.Email = student.Email;
                    existingStudent.AdmissionYear = student.AdmissionYear;
                    existingStudent.TrainingSystemCode = student.TrainingSystemCode;
                    existingStudent.MajorId = student.MajorId;
                    existingStudent.MajorCode = major.MajorCode;
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.StudentId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["MajorId"] = new SelectList(_context.Majors, "MajorId", "MajorName", student.MajorId);
            return View(student);
        }

        // GET: Student/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.Major)
                .FirstOrDefaultAsync(m => m.StudentId == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa sinh viên thành công." });
        }

        // POST: Student/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                _context.Students.Remove(student);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.StudentId == id);
        }
    }
}