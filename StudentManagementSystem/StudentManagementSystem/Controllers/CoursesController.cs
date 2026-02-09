using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Filter;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Controllers
{
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;
        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index(string searchTerm)
        {
            var courses = _context.Courses
                .Where(c => c.IsActive);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                courses = courses.Where(c =>
                c.CourseName.Contains(searchTerm));
            }

            return View(courses.ToList());
        }
        public IActionResult Details()
        {
            return View(_context.Courses.ToList());
        }
         [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // ===================== CREATE COURSE (POST) =====================
        [HttpPost]
        [Authorize(Roles = "Admin")]

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Course course)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please correct the errors in the form.";
                return View(course);
            }

            // Add course
            course.CreatedAt = DateTime.Now;
            course.IsActive = true; // default active
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Course created successfully!";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var course = _context.Courses.Find(id);

            if (course != null)
            {
                // delete related exams first (important)
                var exams = _context.Exams
                    .Where(e => e.CourseId == id)
                    .ToList();

                _context.Exams.RemoveRange(exams);

                _context.Courses.Remove(course);
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }
        // ===================== EDIT COURSE (GET) =====================
        [HttpGet]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
                return NotFound();

            return View(course);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Teacher")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Course course)
        {
            if (id != course.CourseId)
                return NotFound();

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please correct the errors and try again.";
                return View(course);
            }

            try
            {
                var existingCourse = await _context.Courses
                    .FirstOrDefaultAsync(c => c.CourseId == id);

                if (existingCourse == null)
                    return NotFound();

                // Update fields
                existingCourse.CourseName = course.CourseName;
                existingCourse.Description = course.Description;
                existingCourse.TotalCredits = course.TotalCredits;
                existingCourse.Department = course.Department;
                existingCourse.Duration = course.Duration;
                existingCourse.IsActive = course.IsActive;
                existingCourse.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Courses updated successfully!";
                return RedirectToAction(nameof(Index));

                
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Courses.Any(c => c.CourseId == course.CourseId))
                    return NotFound();

                throw;
            }
        }

    }
}
