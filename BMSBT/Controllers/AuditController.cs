using BMSBT.Roles;
using Microsoft.AspNetCore.Mvc;

namespace BMSBT.Controllers
{
    public class AuditController : Controller
    {
        
        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.Username = HttpContext.Session.GetString("UserName");
            ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");
            return View();
        }
    }
}
