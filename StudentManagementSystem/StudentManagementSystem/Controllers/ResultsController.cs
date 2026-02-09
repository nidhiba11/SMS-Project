using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Models;
using System.Security.Claims;

namespace StudentManagementSystem.Controllers
{
    public class ResultsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ResultsController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var results = _context.Results
           .Include(r => r.Student)
               .ThenInclude(s => s.User)
           .Include(r => r.Course)
           .Include(r => r.Exam)
           .Include(r => r.Teacher)
               .ThenInclude(t => t.User)
           .Where(r => r.IsActive)
           .ToList();

            return View(results);
        }
        public IActionResult Details(int id)
        {
            var result = _context.Results
        .Include(r => r.Student)
            .ThenInclude(s => s.User)
        .Include(r => r.Course)
        .Include(r => r.Exam)
        .Include(r => r.Teacher)
            .ThenInclude(t => t.User)
        .FirstOrDefault(r => r.ResultId == id);

            if (result == null)
                return NotFound();

            return View(result);
        }
        [RoleAuthorize("Teacher")]
        public IActionResult TeacherIndex()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            var teacherId = _context.Teachers
                .Where(t => t.UserId == userId)
                .Select(t => t.TeacherId)
                .FirstOrDefault();

            if (teacherId == 0)
            {
                return Unauthorized();
            }

            var results = _context.Results
                .Include(r => r.Student)
                .Include(r => r.Exam)
                .Where(r => r.TeacherId == teacherId)
                .ToList();

            return View(results);
        }
        [RoleAuthorize("Teacher", "Admin")]
        public IActionResult Manage(int examId)
        {
            var teacherId = 1;

            var results = _context.Results
                .Include(r => r.Student)
                .Include(r => r.Course)
                .Where(r => r.ExamId == examId && r.TeacherId == teacherId)
                .ToList();

            return View(results);
        }
        [RoleAuthorize("Admin")]
        public IActionResult AdminIndex()
        {
            var results = _context.Results
        .Include(r => r.Student)
        .Include(r => r.Exam)
        .ToList();

            return View(results ?? new List<Result>());
        }
        public IActionResult ToggleExamPublish(int examId)
        {
            var exam = _context.Exams.Find(examId);

            if (exam == null)
                return NotFound();

            exam.IsPublished = !exam.IsPublished;
            _context.SaveChanges();

            return RedirectToAction("Index", "Exams");
        }
    }
}
