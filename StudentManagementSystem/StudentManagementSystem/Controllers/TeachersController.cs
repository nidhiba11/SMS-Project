using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using StudentManagementSystem.Models;
using StudentManagementSystem.Models.ViewModels;
using System.IO;
using System.Linq;

namespace StudentManagementSystem.Controllers
{
    public class TeachersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TeachersController(ApplicationDbContext context)
        {
            _context = context;
        }
        [RoleAuthorize("Student", "Teacher", "Admin")]
        public IActionResult Index()
        {
            var teachers = _context.Teachers
                .Include(t => t.User)
                .ToList();

            return View(teachers);
        }

        // Create - Display form
        public IActionResult Create()
        {
            // Only users not yet assigned as teachers
            var assignedUserIds = _context.Teachers.Select(t => t.UserId).ToList();
            var availableUsers = _context.Users
                .Where(u => u.Role == "Teacher" && !assignedUserIds.Contains(u.UserId))
                .ToList();

            ViewBag.Users = new SelectList(availableUsers, "UserId", "FullName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Teacher teacher, IFormFile PhotoFile)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Users = new SelectList(_context.Users, "UserId", "FullName", teacher.UserId);
                return View(teacher);
            }

            try
            {
                // Photo upload
                if (PhotoFile != null && PhotoFile.Length > 0)
                {
                    string fileName = Guid.NewGuid() + Path.GetExtension(PhotoFile.FileName);
                    string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Teachers", fileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                        PhotoFile.CopyTo(stream);

                    teacher.Photo = "/Teachers/" + fileName;
                }

                _context.Teachers.Add(teacher);
                _context.SaveChanges();

                TempData["msg"] = "Teacher created successfully!";
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["msg"] = "Error creating teacher!";
                ViewBag.Users = new SelectList(_context.Users, "UserId", "FullName", teacher.UserId);
                return View(teacher);
            }
        }
        // Edit - Display form
        public IActionResult Edit(int id)
        {
            var teacher = _context.Teachers
                .Include(t => t.User)
                .FirstOrDefault(t => t.TeacherId == id);

            if (teacher == null)
                return NotFound();

            // Populate dropdown
            ViewBag.Users = new SelectList(_context.Users, "UserId", "FullName", teacher.UserId);

            return View(teacher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Teacher teacher, IFormFile PhotoFile)
        {
            var existingTeacher = _context.Teachers
                .Include(t => t.User)
                .FirstOrDefault(t => t.TeacherId == teacher.TeacherId);

            if (existingTeacher == null)
                return NotFound();

            // Same pattern as Student Edit
            existingTeacher.Qualification = teacher.Qualification;
            existingTeacher.Department = teacher.Department;
            existingTeacher.Experience = teacher.Experience;
            existingTeacher.Bio = teacher.Bio;

            // Update User (because your view edits it)
            if (existingTeacher.User != null && teacher.User != null)
            {
                existingTeacher.User.FullName = teacher.User.FullName;
                existingTeacher.User.Email = teacher.User.Email;
            }

            // Photo upload (same logic style as Student)
            if (PhotoFile != null && PhotoFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(existingTeacher.Photo))
                {
                    var oldPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        existingTeacher.Photo.TrimStart('/')
                    );

                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                var fileName = Guid.NewGuid() + Path.GetExtension(PhotoFile.FileName);
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Teachers", fileName);

                using var stream = new FileStream(path, FileMode.Create);
                PhotoFile.CopyTo(stream);

                existingTeacher.Photo = "/Teachers/" + fileName;
            }

            _context.SaveChanges();
            TempData["msg"] = "Teacher updated successfully!";
            return RedirectToAction("Index");
        }

        // Delete - Confirmation
        public IActionResult Delete(int id)
        {
            var teacher = _context.Teachers
                .Include(t => t.User)
                .FirstOrDefault(t => t.TeacherId == id);

            if (teacher == null)
                return NotFound();

            return View(teacher);
        }

        // Delete POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var teacher = _context.Teachers
                .Include(t => t.User)
                .FirstOrDefault(t => t.TeacherId == id);

            if (teacher == null)
            {
                TempData["msg"] = "Teacher not found!";
                return RedirectToAction("Index");
            }

            try
            {
                // Delete photo
                if (!string.IsNullOrEmpty(teacher.Photo))
                {
                    var photoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", teacher.Photo.TrimStart('/'));
                    if (System.IO.File.Exists(photoPath))
                        System.IO.File.Delete(photoPath);
                }

                _context.Teachers.Remove(teacher);

                // Optionally delete user
                if (teacher.User != null)
                    _context.Users.Remove(teacher.User);

                _context.SaveChanges();
                TempData["msg"] = "Teacher and associated user deleted successfully!";
            }
            catch
            {
                TempData["msg"] = "Error deleting teacher!";
            }

            return RedirectToAction("Index");
        }

        // Dashboard
        public IActionResult Dashboard()
        {
            string userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdStr);

            var teacher = _context.Teachers
                .Include(t => t.User)
                .FirstOrDefault(t => t.UserId == userId);

            if (teacher == null)
                return NotFound();

            TeacherDashboardVM vm = new TeacherDashboardVM
            {
                FullName = teacher.User.FullName,
                Department = teacher.Department,
                Qualification = teacher.Qualification,
                Experience = teacher.Experience,
                Photo = teacher.Photo,
                TotalExams = _context.Exams.Where(e => e.CourseId != null).Count(),
                ResultsEntered = _context.Results.Where(r => r.TeacherId == teacher.TeacherId).Count(),
                PublishedExams = _context.Exams.Where(e => e.IsPublished).Count(),
                StudentsEvaluated = _context.Results.Where(r => r.TeacherId == teacher.TeacherId).Select(r => r.StudentId).Distinct().Count()
            };

            return View(vm);
        }
    }
}
