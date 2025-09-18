using Microsoft.AspNetCore.Mvc;

namespace BMSBT.Controllers
{
    public class SSQController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult GetBillsSelection()
        {
            return View();
        }

    }
}
