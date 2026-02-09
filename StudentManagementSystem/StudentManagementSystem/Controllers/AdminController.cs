using Microsoft.AspNetCore.Mvc;
using StudentManagementSystem.Models.ViewModels;

namespace StudentManagementSystem.Controllers
{
    [RoleAuthorize("Admin")]

    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult AdminDashboard()
        {
            //  if (HttpContext.Session.GetString("Role") != "Admin")
            //    return Unauthorized();

            AdminDashboardVM vm = new AdminDashboardVM
            {
                TotalStudents = _context.Students.Count(),

                TotalTeachers = _context.Teachers.Count(),

                TotalCourses = _context.Courses
                                       .Where(c => c.IsActive)
                                       .Count(),

                ActiveUsers = _context.Users
                                      .Where(u => u.IsActive)
                                      .Count(),

                PublishedExams = _context.Exams
                                         .Where(e => e.IsPublished)
                                         .Count(),

                PendingResults = _context.Results
                                         .Where(r => !r.IsActive)
                                         .Count()
            };

            return View(vm);
        }
    }
}
