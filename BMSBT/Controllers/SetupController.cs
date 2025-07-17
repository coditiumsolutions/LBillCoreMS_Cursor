using BMSBT.Models;
using BMSBT.Roles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
namespace BMSBT.Controllers
{
   
    public class SetupController : Controller
    {
        private readonly BmsbtContext db;

        public SetupController(BmsbtContext context)
        {
            db = context;
        }

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

        public IActionResult AllTarrifs()
        {
            var data = db.Tarrifs.ToList();
            return View(data);
        }

    }
}
