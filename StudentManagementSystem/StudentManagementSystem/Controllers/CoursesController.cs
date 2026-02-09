using Microsoft.AspNetCore.Mvc;

namespace StudentManagementSystem.Controllers
{
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;
        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View(_context.Courses.Where(c => c.IsActive).ToList());
        }
        [RoleAuthorize("Admin")]
        public IActionResult Details()
        {
            return View(_context.Courses.ToList());
        }
       // [RoleAuthorize("Admin")]
        public IActionResult create()
        {
            return View();
        }
        [HttpPost]
       // [RoleAuthorize("Admin")]
        public IActionResult Create()
        {
            return View();
        }
    }
}
