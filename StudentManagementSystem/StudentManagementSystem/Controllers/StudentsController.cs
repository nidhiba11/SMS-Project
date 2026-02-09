using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Models;
using StudentManagementSystem.Models.ViewModels;
using Microsoft.AspNetCore.Http;

namespace StudentManagementSystem.Controllers
{
    public class StudentsController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        public StudentsController(ApplicationDbContext context, IWebHostEnvironment env) 
        {
            _context = context;
            _environment = env;

        }
        [RoleAuthorize("Admin", "Teacher")]
        public IActionResult Index()
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

            var student = _context.Students
                .Include(s => s.User)
                .FirstOrDefault(s => s.UserId == userId);

            if (student == null)
                return NotFound("Student not found for this user");

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
        [RoleAuthorize("Admin")]
        public IActionResult Details()
        {
            var student = _context.Students
                .Include(s => s.User)
                .Include(s => s.Course)
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
