using BMSBT.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using X.PagedList.Extensions;

namespace BMSBT.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BmsbtContext context;
        private readonly PasswordHasher<User> _passwordHasher;
        public HomeController(ILogger<HomeController> logger, BmsbtContext context)
        {
            _logger = logger;
            this.context = context;
            _passwordHasher = new PasswordHasher<User>();
        }



        //[Authorize]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }

            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");
            return View();
        }



        //[HttpGet]
        //[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        //public IActionResult Index()
        //{
        //    return View();
        //}

        public IActionResult Home()
        {
            var data = context.Users.ToList();
            return View(data);
        }

        public IActionResult Users(int? page)
        {
            int pageSize = 10; // Number of records per page
            int pageNumber = page ?? 1; // Default to page 1 if no page is specified

            var data = context.Users.ToList().ToPagedList(pageNumber, pageSize);
            return View(data);
        }


        public IActionResult Customers()
        {
            var data = context.CustomersDetails.ToList();
            return View(data);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }












        [HttpGet]
        public IActionResult CreateUser()
        {
            return View();
        }




        [HttpPost]
        public IActionResult CreateUser(User user, List<string> Role)
        {
            if (Role != null && Role.Count > 0)
            {
                user.Role = string.Join(",", Role); // Store roles as comma-separated string
            }

            // Hash the password before saving
            user.PasswordHash = _passwordHasher.HashPassword(user, user.PasswordHash);

            context.Users.Add(user);
            context.SaveChanges();

            return RedirectToAction("Users");
        }


        [HttpGet]
        public IActionResult EditUser(int id)
        {
            var user = context.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }

            // If Role is not null, convert it into a list for multi-selection
            ViewBag.SelectedRoles = user.Role?.Split(',') ?? new string[] { };

            return View(user);
        }

        [HttpPost]

        public IActionResult EditUser(User user, string[] Role, string? newPassword)
        {
            var existingUser = context.Users.FirstOrDefault(u => u.Uid == user.Uid);
            if (existingUser == null)
            {
                return NotFound();
            }

            existingUser.EmployeeId = user.EmployeeId;
            existingUser.Username = user.Username;
            existingUser.Role = Role != null ? string.Join(",", Role) : null;

            // Hash new password only if provided
            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                existingUser.PasswordHash = _passwordHasher.HashPassword(existingUser, user.PasswordHash);
            }

            context.SaveChanges();
            return RedirectToAction("Users");
        }






    }
}
