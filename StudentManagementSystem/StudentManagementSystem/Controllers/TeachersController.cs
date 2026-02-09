using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Models;
using StudentManagementSystem.Models.ViewModels;

namespace StudentManagementSystem.Controllers
{
    [RoleAuthorize("Admin", "Teacher", "Student")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

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
        [RoleAuthorize("Teacher")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
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

                TotalExams = _context.Exams
                    .Where(e => e.CourseId != null)
                    .Count(),

                ResultsEntered = _context.Results
                    .Where(r => r.TeacherId == teacher.TeacherId)
                    .Count(),

                PublishedExams = _context.Exams
                    .Where(e => e.IsPublished)
                    .Count(),

                StudentsEvaluated = _context.Results
                    .Where(r => r.TeacherId == teacher.TeacherId)
                    .Select(r => r.StudentId)
                    .Distinct()
                    .Count()
            };

            return View(vm);
        }
        [HttpPost]
       // [RoleAuthorize("Admin")]
        public IActionResult Edit(Teacher teacher, IFormFile PhotoFile)
        {
            try
            {
                if (PhotoFile != null && PhotoFile.Length > 0)
                {
                    string fileName = Guid.NewGuid() + Path.GetExtension(PhotoFile.FileName);
                    string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Teachers", fileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        PhotoFile.CopyTo(stream);
                    }

                    teacher.Photo = fileName;
                }

                _context.Update(teacher);
                _context.SaveChanges();

                TempData["msg"] = "Successfully edited";
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["msg"] = "Sorry! Try again";
                return RedirectToAction("Edit", new { id = teacher.TeacherId });
            }
        }
        [RoleAuthorize("Admin")]
        public IActionResult Details()
        {
            var teachers = _context.Teachers
                .Include(t => t.User)
                .ToList();

            return View(teachers);
        }
    }
}
