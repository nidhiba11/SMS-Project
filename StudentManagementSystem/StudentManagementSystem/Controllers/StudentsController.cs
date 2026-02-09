using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Models;
using StudentManagementSystem.Models.ViewModels;
using System;
using System.IO;
using System.Linq;

namespace StudentManagementSystem.Controllers
{
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        public StudentsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ================= INDEX =================
        public IActionResult Index(string searchTerm)
        {
            var students = _context.Students
                .Include(s => s.User)
                .Include(s => s.Course)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                students = students.Where(s =>
                    s.EnrollmentNo.ToLower().Contains(searchTerm) ||
                    s.User.FullName.ToLower().Contains(searchTerm) ||
                    s.User.Email.ToLower().Contains(searchTerm));
            }

            return View(students.ToList());
        }
        // ================= DETAILS =================
        public IActionResult Details() 
        { 
            var students = _context.Students 
                
                .Include(s => s.User) 
                .Include(s => s.Course) 
                .ToList(); return View(students); 
        }

        // ================= DASHBOARD =================
        [RoleAuthorize("Student")]
        public IActionResult Dashboard()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdStr);

            var student = _context.Students
                .Include(s => s.User)
                .Include(s => s.Course)
                .FirstOrDefault(s => s.UserId == userId);

            if (student == null)
                return NotFound();

            var vm = new StudentDashboardVM
            {
                StudentId = student.StudentId,
                EnrollmentNo = student.EnrollmentNo,
                Semester = student.Semester,
                DOB = student.DOB,
                Photo = student.Photo,
                CreatedAt = student.CreatedAt,
                StudentName = student.User.FullName,
                Age = DateTime.Now.Year - student.DOB.Year
            };

            return View(vm);
        }

        // ================= CREATE GET =================
        [HttpGet]
        public IActionResult Create()
        {
            var usedUserIds = _context.Students.Select(s => s.UserId).ToList();

            ViewBag.Users = _context.Users
                .Where(u => u.Role == "Student" && !usedUserIds.Contains(u.UserId))
                .ToList();

            ViewBag.Courses = _context.Courses.ToList();   

            ViewBag.AutoEnrollment = GenerateEnrollmentNo();
            LoadCreateDropdowns();
            return View();
        }


        // ================= CREATE POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student, IFormFile PhotoFile)
        {
            if (!ModelState.IsValid)
            {
                LoadCreateDropdowns();
                return View(student);
            }

            // ✅ PHOTO UPLOAD
            if (PhotoFile != null && PhotoFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "students");
                Directory.CreateDirectory(uploadsFolder);

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(PhotoFile.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await PhotoFile.CopyToAsync(stream);
                }

                // ✅ DB માં ફક્ત path
                student.Photo = "/uploads/students/" + fileName;
            }

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            TempData["msg"] = "Student created successfully!";
            return RedirectToAction("Index");
        }
        private void LoadCreateDropdowns()
        {
            var usedUserIds = _context.Students.Select(s => s.UserId).ToList();

            ViewBag.Users = _context.Users
                .Where(u => u.Role == "Student" && !usedUserIds.Contains(u.UserId))
                .ToList();

            ViewBag.Courses = _context.Courses.ToList();
        }




        // ================= EDIT GET =================
        public IActionResult Edit(int id)
        {
            var student = _context.Students
        .Include(s => s.User)
        .Include(s => s.Course)
        .FirstOrDefault(s => s.StudentId == id);

            if (student == null)
                return NotFound();

            ViewBag.Courses = _context.Courses.ToList();

            return View(student);
        }

        // ================= EDIT POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Student student, IFormFile PhotoFile)
        {
            var existing = _context.Students.FirstOrDefault(s => s.StudentId == student.StudentId);
            if (existing == null) return NotFound();

            existing.EnrollmentNo = student.EnrollmentNo;
            existing.DOB = student.DOB;
            existing.CourseId = student.CourseId;
            existing.Semester = student.Semester;

            if (PhotoFile != null && PhotoFile.Length > 0)
            {
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid() + Path.GetExtension(PhotoFile.FileName);
                string path = Path.Combine(folder, fileName);

                using var stream = new FileStream(path, FileMode.Create);
                await PhotoFile.CopyToAsync(stream);

                existing.Photo = "/uploads/" + fileName;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ================= DELETE GET =================
        public IActionResult Delete(int id)
        {
            var student = _context.Students
                .Include(s => s.User)
                .FirstOrDefault(s => s.StudentId == id);

            if (student == null) return NotFound();
            return View(student);
        }

        // ================= DELETE POST =================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var student = _context.Students
                .Include(s => s.User)
                .FirstOrDefault(s => s.StudentId == id);

            if (student == null) return NotFound();

            _context.Students.Remove(student);
            if (student.User != null)
                _context.Users.Remove(student.User);

            _context.SaveChanges();
            TempData["msg"] = "Student deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ================= HELPERS =================
        private void LoadDropdowns()
        {
            var usedUserIds = _context.Students.Select(s => s.UserId).ToList();

            ViewBag.Users = _context.Users
                .Where(u => u.Role == "Student" && !usedUserIds.Contains(u.UserId))
                .ToList();

            ViewBag.Courses = _context.Courses.ToList();
        }

        private string GenerateEnrollmentNo()
        {
            int year = DateTime.Now.Year;
            var last = _context.Students
                .OrderByDescending(s => s.StudentId)
                .FirstOrDefault();

            int next = last == null ? 1 : last.StudentId + 1;
            return $"ENR{year}-{next:D4}";
        }
    }
}
