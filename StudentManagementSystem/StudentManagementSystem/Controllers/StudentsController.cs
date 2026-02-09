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

        public StudentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= INDEX (SEARCH) =================
        public IActionResult Index(string searchTerm)
        {
            ViewBag.SearchTerm = searchTerm;

            var students = _context.Students
                .Include(s => s.User)
                .Include(s => s.Course)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
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
                .ToList();

            return View(students);
        }
        [RoleAuthorize("Student")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Dashboard()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

        // ================= CREATE GET =================
        public IActionResult Create()
        {
            PopulateCreateEditDropdowns();
            return View();
        }

        // ================= CREATE POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Student student, IFormFile PhotoFile)
        {
            // ----- Validations -----
            if (student.UserId == 0)
                ModelState.AddModelError("UserId", "Please select a user.");

            if (student.CourseId == 0)
                ModelState.AddModelError("CourseId", "Please select a course.");

            bool studentExists = _context.Students.Any(s => s.UserId == student.UserId);
            if (studentExists)
                ModelState.AddModelError("", "A student already exists for the selected user.");

            if (!ModelState.IsValid)
            {
                PopulateCreateEditDropdowns();
                return View(student);
            }

            // ----- Photo Upload -----
            if (PhotoFile != null && PhotoFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(PhotoFile.FileName);
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                    PhotoFile.CopyTo(stream);

                student.Photo = "/uploads/" + fileName;
            }

            // ----- Save -----
            student.CreatedAt = DateTime.Now;
            _context.Students.Add(student);
            _context.SaveChanges();

            TempData["msg"] = "Student created successfully!";
            return RedirectToAction("Index");
        }

        // ================= EDIT GET =================
        public IActionResult Edit(int id)
        {
            var student = _context.Students
                .Include(s => s.User)
                .Include(s => s.Course)
                .FirstOrDefault(s => s.StudentId == id);

            if (student == null) return NotFound();

            PopulateCreateEditDropdowns();
            return View(student);
        }

        // ================= EDIT POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Student student, IFormFile PhotoFile)
        {
            try
            {
                var existingStudent = _context.Students
                    .Include(s => s.User)
                    .FirstOrDefault(s => s.StudentId == student.StudentId);

                if (existingStudent == null) return NotFound();

                existingStudent.EnrollmentNo = student.EnrollmentNo;
                existingStudent.DOB = student.DOB;
                existingStudent.CourseId = student.CourseId;

                // ----- Photo Upload -----
                if (PhotoFile != null && PhotoFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(existingStudent.Photo))
                    {
                        var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingStudent.Photo.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    string fileName = Guid.NewGuid() + Path.GetExtension(PhotoFile.FileName);
                    string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                        PhotoFile.CopyTo(stream);

                    existingStudent.Photo = "/uploads/" + fileName;
                }

                _context.SaveChanges();
                TempData["msg"] = "Student updated successfully!";
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["msg"] = "Error updating student!";
                return RedirectToAction("Edit", new { id = student.StudentId });
            }
        }

        // ================= DELETE GET =================
       
        public IActionResult Delete(int id)
        {
            var student = _context.Students
                .Include(s => s.User)
                .Include(s => s.Course)
                .FirstOrDefault(s => s.StudentId == id);

            if (student == null)
                return NotFound();

            return View(student);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var student = _context.Students
                                  .Include(s => s.User)
                                  .FirstOrDefault(s => s.StudentId == id);

            if (student == null)
            {
                TempData["msg"] = "Student not found!";
                return RedirectToAction("Index");
            }

            try
            {
                // Remove dependent Results first (if any)
                var results = _context.Results.Where(r => r.StudentId == id).ToList();
                if (results.Any())
                    _context.Results.RemoveRange(results);

                // Delete student photo
                if (!string.IsNullOrEmpty(student.Photo))
                {
                    var photoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", student.Photo.TrimStart('/'));
                    if (System.IO.File.Exists(photoPath))
                        System.IO.File.Delete(photoPath);
                }

                // Remove student
                _context.Students.Remove(student);

                // Remove associated User
                if (student.User != null)
                    _context.Users.Remove(student.User);

                _context.SaveChanges();

                TempData["msg"] = "Student and associated user deleted successfully!";
            }
            catch
            {
                TempData["msg"] = "Error deleting student!";
            }

            return RedirectToAction("Index");
        }

        // ================= DASHBOARD =================
        public IActionResult Dashboard()
        {
            string userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdStr);
            var student = _context.Students
                .Include(s => s.User)
                .Include(s => s.Course)
                .FirstOrDefault(s => s.UserId == userId);

            if (student == null) return NotFound();

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


        [HttpPost]
        [RoleAuthorize("Admin")]
        public IActionResult Delete(int id)
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return Unauthorized();

            var student = _context.Students.Find(id);
            if (student == null)
                return NotFound();

            _context.Students.Remove(student);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // ================= PRIVATE HELPER =================
        private void PopulateCreateEditDropdowns()
        {
            var assignedUserIds = _context.Students.Select(s => s.UserId).ToList();
            var availableUsers = _context.Users
                .Where(u => u.Role == "Student" && !assignedUserIds.Contains(u.UserId))
                .ToList();

            return View(student);
        }
        private string GenerateEnrollmentNo()
        {
            var year = DateTime.Now.Year;

            var lastEnrollment = _context.Students
                .Where(s => s.EnrollmentNo.StartsWith("ENR" + year))
                .OrderByDescending(s => s.EnrollmentNo)
                .Select(s => s.EnrollmentNo)
                .FirstOrDefault();

            int nextNumber = 1;

            if (lastEnrollment != null)
            {
                var parts = lastEnrollment.Split('-');
                nextNumber = int.Parse(parts[1]) + 1;
            }

            return $"ENR{year}-{nextNumber.ToString("D4")}";
        }

        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("Role") ?? "";

            var usedUserIds = _context.Students
                                      .Select(s => s.UserId)
                                      .ToList();
            ViewBag.AutoEnrollment = GenerateEnrollmentNo();
            var studentUsers = _context.Users
                .Where(u => u.Role == "Student" && !usedUserIds.Contains(u.UserId))
                .ToList();

            ViewBag.Users = studentUsers;
            ViewBag.Courses = _context.Courses.ToList();

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student, IFormFile PhotoFile)
        {
            var role = HttpContext.Session.GetString("Role") ?? "";

            if (role != "Admin")
            {
                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                // Photo upload
                if (PhotoFile != null && PhotoFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads/students");
                    Directory.CreateDirectory(uploadsFolder);

                    string fileName = Guid.NewGuid() + Path.GetExtension(PhotoFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await PhotoFile.CopyToAsync(stream);
                    }

                    student.Photo = "/uploads/students/" + fileName;
                }

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            // Reload dropdowns if validation fails
            ViewBag.Users = _context.Users.ToList();
            ViewBag.Courses = _context.Courses.ToList();

            return View(student);
        }
        public IActionResult Edit()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Edit(Student student, IFormFile PhotoFile)
        {
            try
            {
                if (PhotoFile != null && PhotoFile.Length > 0)
                {
                    string fileName = Guid.NewGuid() + Path.GetExtension(PhotoFile.FileName);
                    string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Photos", fileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        PhotoFile.CopyTo(stream);
                    }

                    student.Photo = fileName;
                }

                _context.Update(student);
                _context.SaveChanges();

                TempData["msg"] = "Successfully edited";
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["msg"] = "Sorry! Try again";
                return RedirectToAction("Edit", new { id = student.StudentId });
            }
        }
    }
}
