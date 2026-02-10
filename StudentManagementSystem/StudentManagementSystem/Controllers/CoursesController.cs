using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StudentManagementSystem.Controllers
{
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= INDEX =================
        public IActionResult Index(string searchTerm)
        {
            // Get all active courses
            var courses = _context.Courses
                .Where(c => c.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();

                courses = courses.Where(c =>
                    c.CourseName.ToLower().Contains(searchTerm) ||
                    c.Department.ToLower().Contains(searchTerm) ||
                    c.Description.ToLower().Contains(searchTerm) ||
                    c.Duration.ToString().Contains(searchTerm) ||   // search by duration
                    c.TotalCredits.ToString().Contains(searchTerm)  // search by credits
                );
            }

            ViewBag.SearchTerm = searchTerm; // keep search value
            return View(courses.ToList());
        }

        // ================= DETAILS =================
        public IActionResult Details()
        {
            var courses = _context.Courses.ToList();
            return View(courses);
        }

        // ================= CREATE GET =================
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // ================= CREATE POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Course course)
        {
            if (!ModelState.IsValid)
            {
                return View(course);
            }

            course.CreatedAt = DateTime.Now;
            course.IsActive = course.IsActive; // checkbox binding
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Course created successfully!";
            return RedirectToAction("Details");
        }

        // ================= EDIT GET =================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            return View(course);
        }

        // ================= EDIT POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Course course)
        {
            if (!ModelState.IsValid) return View(course);

            var existing = await _context.Courses.FindAsync(course.CourseId);
            if (existing == null) return NotFound();

            existing.CourseName = course.CourseName;
            existing.Description = course.Description;
            existing.Duration = course.Duration;
            existing.TotalCredits = course.TotalCredits;
            existing.Department = course.Department;
            existing.IsActive = course.IsActive;
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Course updated successfully!";
            return RedirectToAction("Details");
        }

        // ================= DELETE =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Course deleted successfully!";
            return RedirectToAction("Details");
        }
    }
}
