using BMSBT.Models;
using Microsoft.AspNetCore.Mvc;

namespace BMSBT.Controllers
{
    public class UsersController : Controller
    {
        private readonly BmsbtContext _context;

        public UsersController(BmsbtContext context)
        {
            _context = context; 
        }

        public IActionResult Index()
        {
            var data = _context.Users.ToList();
            return View(data);
        }

        public IActionResult AllUsers()
        {
            var data = _context.Users.ToList();
            return View(data);
        }
    }
}
