using BMSBT.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;

namespace BMSBT.Controllers
{
    public class GraphsController : Controller
    {

        private readonly BmsbtContext _context;
        public GraphsController(BmsbtContext context)
        {
            _context = context;

        }
        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.Username = HttpContext.Session.GetString("UserName");
            ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");

            // Grouping by 'Project' column and counting the total customers for each project
            var projectData = _context.CustomersDetails
                .GroupBy(c => c.Project) // Group by Project column
                .Select(g => new
                {
                    ProjectName = g.Key, // Project Name
                    TotalCustomers = g.Count() // Renaming Count to TotalCustomers
                })
                .ToList();

            // Extracting labels (Project names) and data (Total customers per project)
            List<string> labels = projectData.Select(x => x.ProjectName).ToList();
            List<int> data = projectData.Select(x => x.TotalCustomers).ToList();

            // Passing data to View
            ViewBag.ChartLabels = labels;
            ViewBag.ChartData = data;

            //ViewBag.TotalCustomers = totalCustomers; // Send total customers count

            return View();
        }




        public IActionResult GraphSelection()
        {
            if (HttpContext.Session.GetInt32("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.Username = HttpContext.Session.GetString("UserName");
            ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");

            // Grouping by 'Project' column and counting the total customers for each project
            var projectData = _context.CustomersDetails
                .GroupBy(c => c.Project) // Group by Project column
                .Select(g => new
                {
                    ProjectName = g.Key, // Project Name
                    TotalCustomers = g.Count() // Renaming Count to TotalCustomers
                })
                .ToList();

            // Extracting labels (Project names) and data (Total customers per project)
            List<string> labels = projectData.Select(x => x.ProjectName).ToList();
            List<int> data = projectData.Select(x => x.TotalCustomers).ToList();

            // Passing data to View
            ViewBag.ChartLabels = labels;
            ViewBag.ChartData = data;

            //ViewBag.TotalCustomers = totalCustomers; // Send total customers count

            return View();
        }






        public IActionResult GraphSelectionEBills()
        {
            return View();
        }


        [HttpGet]
        public JsonResult GetBillingData(string month, string year)
        {
            var billData = _context.ElectricityBills
                .Where(b => b.BillingMonth == month && b.BillingYear == year)
                .GroupBy(b => new { b.BillingMonth, b.BillingYear, b.PaymentStatus })
                .Select(g => new
                {
                    Month = g.Key.BillingMonth,
                    Year = g.Key.BillingYear,
                    PaymentStatus = g.Key.PaymentStatus,
                    TotalAmount = g.Sum(b => b.BillAmountInDueDate ?? 0) // Handle null values
                })
                .ToList();

            


            var result = new
            {
                labels = billData.Select(x => x.PaymentStatus).ToList(),
                data = billData.Select(x => x.TotalAmount).ToList()
            };

            Console.WriteLine($"DEBUG: Returning JSON: {System.Text.Json.JsonSerializer.Serialize(result)}");

            if (!billData.Any())
            {
                return Json(new { labels = new List<string>(), data = new List<int>() });
            }

            return Json(result);
        }















        public IActionResult BillReport()
        {
            return View();
        }

        [HttpPost]
        public JsonResult GetBillData(string month, string year)
        {
            var billData = _context.ElectricityBills
                .Where(b => b.BillingMonth == month && b.BillingYear == year)
                .GroupBy(b => b.PaymentStatus)
                .Select(g => new { PaymentStatus = g.Key, Count = g.Count() })
                .ToList();

            //    var billData = _context.ElectricityBills
            //.GroupBy(b => b.PaymentStatus)
            //.Select(g => new
            //{
            //    PaymentStatus = g.Key,
            //    Count = g.Count()
            //})
            //.ToList();


            var result = new
            {
                Labels = billData.Select(x => x.PaymentStatus).ToList(),
                Data = billData.Select(x => x.Count).ToList()
            };

            Console.WriteLine($"DEBUG: Returning JSON: {System.Text.Json.JsonSerializer.Serialize(result)}"); // Log response

            if (!billData.Any())  // Handle empty data case
            {
                return Json(new { Labels = new List<string>(), Data = new List<int>() });
            }

            return Json(result);
        }





    }
}
