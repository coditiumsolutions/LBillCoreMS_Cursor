using BMSBT.Models;
using BMSBT.Services;
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
        private readonly IAuditLogService _auditLogService;

        public HomeController(
            ILogger<HomeController> logger,
            BmsbtContext context,
            IAuditLogService auditLogService)
        {
            _logger = logger;
            this.context = context;
            _auditLogService = auditLogService;
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
        public async Task<IActionResult> CreateUser(User user, List<string> Role)
        {
            if (Role != null && Role.Count > 0)
            {
                user.Role = string.Join(",", Role);
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, user.PasswordHash);

            context.Users.Add(user);
            HttpContext.Items["SkipEfAudit"] = true;
            try
            {
                await context.SaveChangesAsync();
            }
            finally
            {
                HttpContext.Items.Remove("SkipEfAudit");
            }

            var newData = UserAuditHelper.CreateSnapshot(user, passwordChanged: true);
            await _auditLogService.LogAsync(
                UserAuditHelper.TableName,
                "INSERT",
                user.Uid.ToString(),
                null,
                newData,
                UserAuditHelper.ModuleName);

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
        public async Task<IActionResult> EditUser(User user, string[] Role, string? newPassword)
        {
            var existingUser = context.Users.FirstOrDefault(u => u.Uid == user.Uid);
            if (existingUser == null)
            {
                return NotFound();
            }

            var passwordChanged = !string.IsNullOrEmpty(user.PasswordHash);
            var oldSnapshot = UserAuditHelper.CreateSnapshot(existingUser);

            existingUser.EmployeeId = user.EmployeeId;
            existingUser.Username = user.Username;
            existingUser.Role = Role != null ? string.Join(",", Role) : null;

            if (passwordChanged)
            {
                existingUser.PasswordHash = _passwordHasher.HashPassword(existingUser, user.PasswordHash);
            }

            var newSnapshot = UserAuditHelper.CreateSnapshot(existingUser, passwordChanged);
            var (oldData, newData) = UserAuditHelper.BuildDiff(oldSnapshot, newSnapshot);

            HttpContext.Items["SkipEfAudit"] = true;
            try
            {
                await context.SaveChangesAsync();
            }
            finally
            {
                HttpContext.Items.Remove("SkipEfAudit");
            }

            if (oldData.Count > 0)
            {
                await _auditLogService.LogAsync(
                    UserAuditHelper.TableName,
                    "UPDATE",
                    existingUser.Uid.ToString(),
                    oldData,
                    newData,
                    UserAuditHelper.ModuleName);
            }

            return RedirectToAction("Users");
        }






    }
}
