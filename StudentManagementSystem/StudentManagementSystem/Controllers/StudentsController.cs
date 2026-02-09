using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Models;
using StudentManagementSystem.Models.ViewModels;
using Microsoft.AspNetCore.Http;

namespace StudentManagementSystem.Controllers
{
    [RoleAuthorize("Admin", "Teacher")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

    public class StudentsController : Controller
    {

        private readonly ApplicationDbContext _context;
       // private readonly IWebHostEnvironment _env;
        public StudentsController(ApplicationDbContext context)
        {
            _context = context;

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
            int userId = int.Parse(HttpContext.Session.GetString("UserId"));

            var student = _context.Students
                .FirstOrDefault(s => s.UserId == userId);

            if (student == null)
                return RedirectToAction("Login", "Account");

            var vm = new StudentDashboardVM
            {
                StudentId = student.StudentId,
                EnrollmentNo = student.EnrollmentNo,
                Semester = student.Semester,
                DOB = student.DOB,
                Photo = student.Photo,
                CreatedAt = student.CreatedAt,
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
        /*    public IActionResult Create()
            {
                ViewBag.Users = _context.Users.Where(u => u.Role == "Student").ToList();
                ViewBag.Courses = _context.Courses.Where(c => c.IsActive).ToList();
                return View();
            }

            // CREATE POST
            [HttpPost]
            [ValidateAntiForgeryToken]
            public IActionResult Create(Student student, IFormFile PhotoFile)
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Users = _context.Users.Where(u => u.Role == "Student").ToList();
                    ViewBag.Courses = _context.Courses.Where(c => c.IsActive).ToList();
                    return View(student);
                }

                if (PhotoFile != null && PhotoFile.Length > 0)
                {
                    var uploads = Path.Combine(_env.WebRootPath, "uploads");
                    Directory.CreateDirectory(uploads);

                    var fileName = Guid.NewGuid() + Path.GetExtension(PhotoFile.FileName);
                    var path = Path.Combine(uploads, fileName);

                    using var stream = new FileStream(path, FileMode.Create);
                    PhotoFile.CopyTo(stream);

                    student.Photo = "/uploads/" + fileName;
                }

                student.CreatedAt = DateTime.Now;
                _context.Students.Add(student);
                _context.SaveChanges();

                return RedirectToAction(nameof(Details));
            }  */
       // [RoleAuthorize("Admin")]
        public IActionResult Edit()
        {
            return View();
        }
        [HttpPost]
       // [RoleAuthorize("Admin")]
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
