using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BMSBT.Models;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;
using BMSBT.BillServices;
using BMSBT.Services;
using Microsoft.AspNetCore.Identity;
using System.Text.Json; // Required at the top

namespace BMSBT.Controllers
{
    public class LoginController : Controller
    {
        private readonly BmsbtContext _context;
        private readonly ICurrentOperatorService _operatorService;
        private readonly IAuditLogService _auditLogService;
        private readonly IAuthSessionService _authSessionService;

        public LoginController(
            BmsbtContext context,
            ICurrentOperatorService operatorService,
            IAuditLogService auditLogService,
            IAuthSessionService authSessionService)
        {
            _context = context;
            _operatorService = operatorService;
            _auditLogService = auditLogService;
            _authSessionService = authSessionService;
        }
        MaintenanceBill m = new MaintenanceBill();


        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string username, string password)  // Make the method async
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Username and Password are required.";
                return View();
            }

            // Find user by username
            var user = _context.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                ViewBag.Error = "Invalid username or password.";
                return View();
            }

            // Verify password using PasswordHasher
            var passwordHasher = new PasswordHasher<User>();
            var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

            if (result == PasswordVerificationResult.Success)
            {
                await _authSessionService.PopulateSessionAsync(HttpContext, user);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role ?? string.Empty)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                    AllowRefresh = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    authProperties);

                await _auditLogService.LogAsync(
                    "Users",
                    "LOGIN",
                    user.Uid.ToString(),
                    null,
                    new
                    {
                        user.Username,
                        user.Role,
                        LoginAt = DateTime.Now
                    },
                    "Authentication");

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Invalid username or password.";
            return View();
        }




        public IActionResult AccessDenied()
        {
            return View();
        }


        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }


        private string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }



        //2. Important Login Detail
        //Login Method which uses Cookies Detail
        [Authorize(Roles = "Admin")]
        public IActionResult AdminDashboard()
        {
            return View();
        }



        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            //  if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            if (user != null && user.PasswordHash == password)
            {
                await _authSessionService.PopulateSessionAsync(HttpContext, user);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role ?? "User")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                    AllowRefresh = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                await _auditLogService.LogAsync(
                    "Users",
                    "LOGIN",
                    user.Uid.ToString(),
                    null,
                    new
                    {
                        user.Username,
                        user.Role,
                        LoginAt = DateTime.Now
                    },
                    "Authentication");

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Invalid username or password";
            return View();
        }





    }
}
