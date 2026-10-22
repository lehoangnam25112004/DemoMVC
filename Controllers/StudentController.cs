using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DemoMVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DemoMVC.Data;
using DemoMVC.Models;

namespace DemoMVC.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly StudentCodeGenerator _codeGenerator;

        public StudentController(ApplicationDbContext context, StudentCodeGenerator codeGenerator)
        {
            _context = context;
            _codeGenerator = codeGenerator;
        }

        // GET: Student
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Students.Include(s => s.Major);
            return View(await applicationDbContext.ToListAsync());
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
        public IActionResult Create()
        {
          ViewData["MajorId"] = new SelectList(_context.Majors.OrderBy(m => m.MajorCode),"MajorId","MajorName");
          return View();
        }

        // POST: Student/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        
    [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(
    [Bind("FullName,Address,Email,AdmissionYear,TrainingSystemCode,MajorId")]
    Student student)
{
    // Mã sinh viên và mã ngành do hệ thống tạo/lấy từ khoa
    ModelState.Remove(nameof(Student.StudentCode));
    ModelState.Remove(nameof(Student.MajorCode));

    // Kiểm tra khoa được chọn
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
        // Lấy mã ngành từ bảng Majors
        student.MajorCode = major.MajorCode;

        // Sinh mã sinh viên tự động
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

        // POST: Student/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StudentId,StudentCode,FullName,Address,Email,AdmissionYear,TrainingSystemCode,MajorCode,MajorId")] Student student)
        {
            if (id != student.StudentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(student);
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

        // POST: Student/Delete/5
        [HttpPost, ActionName("Delete")]
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


