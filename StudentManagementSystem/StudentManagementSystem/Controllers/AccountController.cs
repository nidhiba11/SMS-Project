using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Models;
using StudentManagementSystem.Models.ViewModels;
namespace StudentManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        public AccountController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }
        
        // LOGIN PAGE
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _context.Users.FirstOrDefault(u =>
                u.Email == model.Email &&
                u.Password == model.Password &&
                u.IsActive == true);

            if (user == null)
            {
                ViewBag.Error = "Invalid email or password";
                return View(model);
            }

            // Clear old session
            HttpContext.Session.Clear();

            // Store correct values
            HttpContext.Session.SetString("UserId", user.UserId.ToString());
            HttpContext.Session.SetString("Role", user.Role);

            // Redirect by role
            if (user.Role == "Admin")
                return RedirectToAction("AdminDashboard", "Admin");

            if (user.Role == "Teacher")
                return RedirectToAction("Dashboard", "Teachers");

            return RedirectToAction("Dashboard", "Students");
        }


        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return RedirectToAction("Login", "Account");
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Check if email already exists
            var existingUser = _context.Users
                .FirstOrDefault(u => u.Email == model.Email);

            if (existingUser != null)
            {
                ViewBag.Error = "Email already registered";
                return View(model);
            }

            // Create new student user
            var user = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                Password = model.Password, // plain for now
                Role = "Student",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return RedirectToAction("Login");
        }
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Email is required";
                return View();
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.IsActive);

            if (user == null)
            {
                ViewBag.Error = "Email not found";
                return View();
            }

            ViewBag.Success = "Your password is: " + user.Password;

            return View();
        }

    }
}
