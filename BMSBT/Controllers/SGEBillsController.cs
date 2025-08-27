using BMSBT.Models;
using BMSBT.ViewModels;
using Humanizer;
using Microsoft.AspNetCore.Mvc;

namespace BMSBT.Controllers
{
    public class SGEBillsController : Controller
    {
        private readonly BmsbtContext _dbContext;

        public SGEBillsController(BmsbtContext context)
        {
            _dbContext = context;
        }



        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult SGEBills()
        {
            // populate dropdowns
            ViewBag.MonthList = System.Globalization.CultureInfo
                .CurrentCulture
                .DateTimeFormat
                .MonthNames
                .Where(m => !string.IsNullOrEmpty(m))
                .ToList();

            ViewBag.YearList = Enumerable
                .Range(DateTime.Now.Year - 2, 5)
                .Select(y => y.ToString())
                .ToList();

            // default selection = current month/year
            ViewBag.SelectedMonth = DateTime.Now.ToString("MMMM");
            ViewBag.SelectedYear = DateTime.Now.Year.ToString();

            // pass an *empty* model so the view can still render
            return View(new List<BillsViewModel>());
        }

        [HttpPost]
        public IActionResult SGEBills(string btNoSearch, string billingYear, string billingMonth)
        {
            // Save selected filters to ViewBag for form persistence
            ViewBag.SelectedMonth = billingMonth;
            ViewBag.SelectedYear = billingYear;
            ViewBag.BTNoSearch = btNoSearch;

            // Prepare month & year dropdowns
            ViewBag.MonthList = Enumerable.Range(1, 12)
                                          .Select(m => new DateTime(2000, m, 1).ToString("MMMM"))
                                          .ToList();
            ViewBag.YearList = Enumerable.Range(DateTime.Now.Year - 10, 11)
                                         .Select(y => y.ToString())
                                         .ToList();

            // Base query
            var query = _dbContext.ElectricityBills.AsQueryable();

            // Filtering by month & year
            if (!string.IsNullOrEmpty(billingMonth))
                query = query.Where(b => b.BillingMonth == billingMonth);

            if (!string.IsNullOrEmpty(billingYear))
                query = query.Where(b => b.BillingYear == billingYear);

            // Filtering by BT No if provided
            if (!string.IsNullOrEmpty(btNoSearch))
                query = query.Where(b => b.Btno.Contains(btNoSearch));

            var groupedBills = query
             .GroupBy(x => x.Sector)
             .Select(g => new BillsViewModel
             {
                 Sector = g.Key ?? "No Sector",
                 Bills = g.ToList()
             })
             .ToList();


            return View(groupedBills);
        }
        



        // GET View for bill
        [HttpGet]
        public IActionResult SGViewEBill(int id)
        {
            var bill = _dbContext.ElectricityBills.FirstOrDefault(b => b.Uid == id);
           if (bill == null)
                return NotFound();

            return View(bill);
        }



        // POST Update bill status
        [HttpPost]
        public IActionResult SGViewEBill(int id, string billStatus)
        {
            var bill = _dbContext.ElectricityBills.FirstOrDefault(b => b.Uid == id);
            if (bill == null)
                return NotFound();

            //string updatedBy = "Ghauri"; // You can later use User.Identity.Name or Session
                                         // ✅ Get username from session
            string updatedBy = HttpContext.Session.GetString("UserName") ?? "Unknown Operator";


            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            string newLog = $"[{timestamp}] Status changed to \"{billStatus}\" by {updatedBy}";

            // Append log to history
            if (string.IsNullOrEmpty(bill.History))
                bill.History = newLog;
            else
                bill.History += Environment.NewLine + newLog;



            bill.PaymentStatus = billStatus;
           

            _dbContext.SaveChanges();

            TempData["SuccessMessage"] = "Bill status updated successfully.";
            return RedirectToAction("SGViewEBill", new { id = id }); // Go to GET method now
        }


    }
}
