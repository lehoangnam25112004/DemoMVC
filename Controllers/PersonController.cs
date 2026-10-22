using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DemoMVC.Data;
using DemoMVC.Models;

namespace DemoMVC.Controllers
{
    public class PersonController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PersonController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. GET: Person (Lấy dữ liệu từ cả 2 bảng Person và Department)
        public async Task<IActionResult> Index()
        {
            var persons = await _context.Persons
                .Include(p => p.Department) // LINQ Join lấy thông tin Phòng ban
                .AsNoTracking()
                .ToListAsync();

            return View(persons);
        }

        // 2. GET: Person/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var person = await _context.Persons
                .Include(p => p.Department) // Bổ sung Include để xem được Tên phòng ban
                .FirstOrDefaultAsync(m => m.FullName == id);

            if (person == null)
            {
                return NotFound();
            }

            return View(person);
        }

        // 3. GET: Person/Create
        public IActionResult Create()
        {
            // Truyền danh sách Phòng ban sang View để làm DropdownList
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName");
            return View();
        }

        // 4. POST: Person/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Thêm DepartmentId vào Bind
        public async Task<IActionResult> Create([Bind("FullName,Address,Email,DepartmentId")] Person person) 
        {
            if (ModelState.IsValid)
            {
                _context.Add(person);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            // Nếu lỗi, nạp lại danh sách phòng ban cho DropdownList
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", person.DepartmentId);
            return View(person);
        }

        // 5. GET: Person/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var person = await _context.Persons.FindAsync(id);
            if (person == null)
            {
                return NotFound();
            }
            // Truyền danh sách Phòng ban và chọn sẵn giá trị hiện tại
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", person.DepartmentId);
            return View(person);
        }

        // 6. POST: Person/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Thêm DepartmentId vào Bind
        public async Task<IActionResult> Edit(string id, [Bind("FullName,Address,Email,DepartmentId")] Person person)
        {
            if (id != person.FullName)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(person);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PersonExists(person.FullName))
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
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", person.DepartmentId);
            return View(person);
        }

        // 7. GET: Person/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var person = await _context.Persons
                .Include(p => p.Department) // Bổ sung Include để hiển thị thông tin Phòng ban trước khi xóa
                .FirstOrDefaultAsync(m => m.FullName == id);

            if (person == null)
            {
                return NotFound();
            }

            return View(person);
        }

        // 8. POST: Person/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var person = await _context.Persons.FindAsync(id);
            if (person != null)
            {
                _context.Persons.Remove(person);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PersonExists(string id)
        {
            return _context.Persons.Any(e => e.FullName == id);
        }
    }
}