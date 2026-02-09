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

            // Normalize inputs
            var email = model.Email?.Trim().ToLower();
            var password = model.Password?.Trim();

            var user = _context.Users.FirstOrDefault(u =>
                u.Email.ToLower() == email &&
                u.Password == password &&
                u.IsActive);

            if (user == null)
            {
                ViewBag.Error = "Invalid email or password";
                return View(model);
            }

            // Set session
            HttpContext.Session.Clear();
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Role", user.Role);
            HttpContext.Session.SetString("FullName", user.FullName);

            // Redirect by role
            if (user.Role == "Admin")
                return RedirectToAction("AdminDashboard", "Admin");

            if (user.Role == "Teacher")
                return RedirectToAction("Dashboard", "Teachers");

            return RedirectToAction("Dashboard", "Students");
        }

        // LOGOUT
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return RedirectToAction("Login", "Account");
        }

        // REGISTER PAGE
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

            var email = model.Email?.Trim().ToLower();

            // Check if email already exists
            var existingUser = _context.Users
                .FirstOrDefault(u => u.Email.ToLower() == email);

            if (existingUser != null)
            {
                ViewBag.Error = "Email already registered";
                return View(model);
            }

            // Create new student user
            var user = new User
            {
                FullName = model.FullName?.Trim(),
                Email = email,
                Password = model.Password?.Trim(), // plain for now
                Role = "Student",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return RedirectToAction("Login");
        }

        // FORGOT PASSWORD
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

            var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == email.Trim().ToLower() && u.IsActive);

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
