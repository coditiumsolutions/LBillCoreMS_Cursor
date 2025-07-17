using Microsoft.AspNetCore.Mvc;
using BMSBT.Models; // adjust if your model namespace is different
using System.Linq;

namespace BMSBT.Controllers
{
    public class SGEBillingHistoryController : Controller
    {
        private readonly BmsbtContext _dbContext;

        public SGEBillingHistoryController(BmsbtContext dbContext)
        {
            _dbContext = dbContext;
        }


        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult SGEBillHistory(string searchCustomer)
        {
            List<ElectricityBill> bills = new();

            if (!string.IsNullOrWhiteSpace(searchCustomer))
            {
                bills = _dbContext.ElectricityBills
                    .Where(b => b.CustomerNo.Contains(searchCustomer) || b.Btno.Contains(searchCustomer))
                    .OrderByDescending(b => b.BillingYear)
                    .ThenByDescending(b => b.BillingMonth)
                    .ToList();
            }

            ViewBag.SearchValue = searchCustomer;
            return View(bills);
        }
    }
}
