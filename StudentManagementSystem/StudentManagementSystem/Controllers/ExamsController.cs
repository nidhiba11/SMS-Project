using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Controllers
{
    public class ExamsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ExamsController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var examsQuery = _context.Exams
           .Include(e => e.Course)
           .AsQueryable();

            if (User.IsInRole("Student"))
            {
                examsQuery = examsQuery.Where(e => e.IsPublished);
            }
            var exams = await examsQuery
                .OrderByDescending(e => e.ExamDate)
                .ToListAsync();

            return View(exams);
        }
        public async Task<IActionResult> Details(int id)
        {
            var exam = await _context.Exams
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.ExamId == id);

            if (exam == null)
            {
                return NotFound();
            }

            if (User.IsInRole("Student") && !exam.IsPublished)
            {
                return Forbid();
            }

            return View(exam);
        }
        [HttpGet]
        [RoleAuthorize("Admin","Teacher")]
        public IActionResult Create()
        {
            ViewBag.Courses = _context.Courses.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Exam exam)
        {
            // if (!(User.IsInRole("Admin") || User.IsInRole("Teacher")))
            //   return Unauthorized();
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine(error.ErrorMessage);
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Courses = _context.Courses.ToList();
                return View(exam);
            }

            exam.CreatedAt = DateTime.Now;
            exam.IsPublished = false;

            _context.Exams.Add(exam);
            _context.SaveChanges();

            TempData["success"] = "Exam created successfully!";
            return RedirectToAction("Index");
        }


        [RoleAuthorize("Admin","Teacher")]
        public async Task<IActionResult> Edit(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null)
            {
                return NotFound();
            }

            ViewBag.Courses = _context.Courses.ToList();
            return View(exam);
        }

        [HttpPost]
        [RoleAuthorize("Admin", "Teacher")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Exam exam)
        {
            if (id != exam.ExamId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                exam.UpdatedAt = DateTime.Now;
                _context.Update(exam);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Courses = _context.Courses.ToList();
            return View(exam);
        }
    }
}
