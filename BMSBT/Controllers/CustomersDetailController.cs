using Microsoft.AspNetCore.Mvc;

namespace BMSBT.Controllers
{
    public class CustomersDetailController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
