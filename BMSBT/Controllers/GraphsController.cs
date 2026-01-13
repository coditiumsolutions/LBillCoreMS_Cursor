using BMSBT.Models;
using BMSBT.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.VisualBasic;
using System.Linq;

namespace BMSBT.Controllers
{
    public class GraphsController : Controller
    {

        private readonly BmsbtContext _context;
        public GraphsController(BmsbtContext context)
        {
            _context = context;

        }
        /// <summary>
        /// Main graphs dashboard at /Graphs.
        /// Shows total bills and status-wise summary (Paid, Paid with Surcharge, Unpaid).
        /// </summary>
        [HttpGet]
        public IActionResult Index(string? selectedMonth, string? selectedYear)
        {
            if (HttpContext.Session.GetInt32("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }

            ViewBag.Username = HttpContext.Session.GetString("UserName");
            ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");

            // Populate dropdown lists
            var vm = new BillStatusDashboardViewModel
            {
                MonthList = GetMonthList(),
                YearList = GetYearList(),
                SelectedMonth = selectedMonth,
                SelectedYear = selectedYear
            };

            // If month and year are selected, filter bills
            if (!string.IsNullOrWhiteSpace(selectedMonth) && !string.IsNullOrWhiteSpace(selectedYear))
            {
                var billsQuery = _context.ElectricityBills
                    .Where(b => b.BillingMonth == selectedMonth && b.BillingYear == selectedYear);

                int totalBills = billsQuery.Count();
                int paidCount = billsQuery.Count(b => b.PaymentStatus == "Paid");
                int paidWithSurchargeCount = billsQuery.Count(b => b.PaymentStatus == "Paid with Surcharge");
                int unpaidCount = billsQuery.Count(b => b.PaymentStatus == "Unpaid");

                vm.TotalBills = totalBills;
                vm.PaidCount = paidCount;
                vm.PaidWithSurchargeCount = paidWithSurchargeCount;
                vm.UnpaidCount = unpaidCount;
                vm.Labels = new List<string> { "Paid", "Paid with Surcharge", "Unpaid" };
                vm.Counts = new List<int> { paidCount, paidWithSurchargeCount, unpaidCount };
            }
            else
            {
                // No selection - show empty/zero data
                vm.Labels = new List<string> { "Paid", "Paid with Surcharge", "Unpaid" };
                vm.Counts = new List<int> { 0, 0, 0 };
            }

            return View(vm);
        }

        /// <summary>
        /// AJAX endpoint to get bill data for selected month/year
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public JsonResult GetBillStatusData(string month, string year)
        {
            if (string.IsNullOrWhiteSpace(month) || string.IsNullOrWhiteSpace(year))
            {
                return Json(new
                {
                    Labels = new List<string> { "Paid", "Paid with Surcharge", "Unpaid" },
                    Data = new List<int> { 0, 0, 0 },
                    TotalBills = 0,
                    PaidCount = 0,
                    PaidWithSurchargeCount = 0,
                    UnpaidCount = 0
                });
            }

            var billsQuery = _context.ElectricityBills
                .Where(b => b.BillingMonth == month && b.BillingYear == year);

            int totalBills = billsQuery.Count();
            int paidCount = billsQuery.Count(b => b.PaymentStatus == "Paid");
            int paidWithSurchargeCount = billsQuery.Count(b => b.PaymentStatus == "Paid with Surcharge");
            int unpaidCount = billsQuery.Count(b => b.PaymentStatus == "Unpaid");

            return Json(new
            {
                Labels = new List<string> { "Paid", "Paid with Surcharge", "Unpaid" },
                Data = new List<int> { paidCount, paidWithSurchargeCount, unpaidCount },
                TotalBills = totalBills,
                PaidCount = paidCount,
                PaidWithSurchargeCount = paidWithSurchargeCount,
                UnpaidCount = unpaidCount
            });
        }

        /// <summary>
        /// Maintenance Bills dashboard at /Graphs/Maintenance.
        /// Shows total maintenance bills and status-wise summary (Paid, Paid with Surcharge, Unpaid).
        /// </summary>
        [HttpGet]
        public IActionResult Maintenance(string? selectedMonth, string? selectedYear)
        {
            if (HttpContext.Session.GetInt32("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }

            ViewBag.Username = HttpContext.Session.GetString("UserName");
            ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");

            // Populate dropdown lists
            var vm = new BillStatusDashboardViewModel
            {
                MonthList = GetMonthList(),
                YearList = GetYearList(),
                SelectedMonth = selectedMonth,
                SelectedYear = selectedYear
            };

            // If month and year are selected, filter bills
            if (!string.IsNullOrWhiteSpace(selectedMonth) && !string.IsNullOrWhiteSpace(selectedYear))
            {
                var billsQuery = _context.MaintenanceBills
                    .Where(b => b.BillingMonth == selectedMonth && b.BillingYear == selectedYear);

                int totalBills = billsQuery.Count();
                int paidCount = billsQuery.Count(b => b.PaymentStatus == "Paid");
                int paidWithSurchargeCount = billsQuery.Count(b => b.PaymentStatus == "Paid with Surcharge");
                int unpaidCount = billsQuery.Count(b => b.PaymentStatus == "Unpaid");

                vm.TotalBills = totalBills;
                vm.PaidCount = paidCount;
                vm.PaidWithSurchargeCount = paidWithSurchargeCount;
                vm.UnpaidCount = unpaidCount;
                vm.Labels = new List<string> { "Paid", "Paid with Surcharge", "Unpaid" };
                vm.Counts = new List<int> { paidCount, paidWithSurchargeCount, unpaidCount };
            }
            else
            {
                // No selection - show empty/zero data
                vm.Labels = new List<string> { "Paid", "Paid with Surcharge", "Unpaid" };
                vm.Counts = new List<int> { 0, 0, 0 };
            }

            return View(vm);
        }

        /// <summary>
        /// AJAX endpoint to get maintenance bill data for selected month/year
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public JsonResult GetMaintenanceBillStatusData(string month, string year)
        {
            if (string.IsNullOrWhiteSpace(month) || string.IsNullOrWhiteSpace(year))
            {
                return Json(new
                {
                    Labels = new List<string> { "Paid", "Paid with Surcharge", "Unpaid" },
                    Data = new List<int> { 0, 0, 0 },
                    TotalBills = 0,
                    PaidCount = 0,
                    PaidWithSurchargeCount = 0,
                    UnpaidCount = 0
                });
            }

            var billsQuery = _context.MaintenanceBills
                .Where(b => b.BillingMonth == month && b.BillingYear == year);

            int totalBills = billsQuery.Count();
            int paidCount = billsQuery.Count(b => b.PaymentStatus == "Paid");
            int paidWithSurchargeCount = billsQuery.Count(b => b.PaymentStatus == "Paid with Surcharge");
            int unpaidCount = billsQuery.Count(b => b.PaymentStatus == "Unpaid");

            return Json(new
            {
                Labels = new List<string> { "Paid", "Paid with Surcharge", "Unpaid" },
                Data = new List<int> { paidCount, paidWithSurchargeCount, unpaidCount },
                TotalBills = totalBills,
                PaidCount = paidCount,
                PaidWithSurchargeCount = paidWithSurchargeCount,
                UnpaidCount = unpaidCount
            });
        }

        private List<SelectListItem> GetMonthList()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "January", Text = "January" },
                new SelectListItem { Value = "February", Text = "February" },
                new SelectListItem { Value = "March", Text = "March" },
                new SelectListItem { Value = "April", Text = "April" },
                new SelectListItem { Value = "May", Text = "May" },
                new SelectListItem { Value = "June", Text = "June" },
                new SelectListItem { Value = "July", Text = "July" },
                new SelectListItem { Value = "August", Text = "August" },
                new SelectListItem { Value = "September", Text = "September" },
                new SelectListItem { Value = "October", Text = "October" },
                new SelectListItem { Value = "November", Text = "November" },
                new SelectListItem { Value = "December", Text = "December" }
            };
        }

        private List<SelectListItem> GetYearList()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "2025", Text = "2025" },
                new SelectListItem { Value = "2026", Text = "2026" }
            };
        }




        public IActionResult GraphSelection()
        {
            if (HttpContext.Session.GetInt32("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.Username = HttpContext.Session.GetString("UserName");
            ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");

            // Prepare ViewModel with dropdown lists
            var vm = new BillStatusDashboardViewModel
            {
                MonthList = GetMonthList(),
                YearList = GetYearList()
            };

            return View(vm);
        }

        /// <summary>
        /// AJAX endpoint to get bill status data based on bill type, month, and year
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public JsonResult GetSelectionBillStatusData(string billType, string month, string year)
        {
            if (string.IsNullOrWhiteSpace(billType) || string.IsNullOrWhiteSpace(month) || string.IsNullOrWhiteSpace(year))
            {
                return Json(new
                {
                    Labels = new List<string> { "Paid", "Paid with Surcharge", "Unpaid" },
                    Data = new List<int> { 0, 0, 0 },
                    TotalBills = 0,
                    PaidCount = 0,
                    PaidWithSurchargeCount = 0,
                    UnpaidCount = 0,
                    CustomerLabels = new List<string> { "Total Customers", "Paid Customers", "Unpaid Customers" },
                    CustomerData = new List<int> { 0, 0, 0 },
                    TotalCustomersCount = 0,
                    PaidCustomersCount = 0,
                    UnpaidCustomersCount = 0,
                    AmountLabels = new List<string> { "Generated Amount", "Received Amount", "Remaining Amount" },
                    AmountData = new List<decimal> { 0, 0, 0 },
                    GeneratedAmount = 0,
                    ReceivedAmount = 0,
                    RemainingAmount = 0
                });
            }

            int totalBills = 0;
            int paidCount = 0;
            int paidWithSurchargeCount = 0;
            int unpaidCount = 0;
            int totalCustomersCount = 0;
            int paidCustomersCount = 0;
            int unpaidCustomersCount = 0;
            decimal generatedAmount = 0;
            decimal receivedAmount = 0;
            decimal remainingAmount = 0;

            // Select the appropriate table based on bill type
            if (billType == "Electricity Bills")
            {
                var billsQuery = _context.ElectricityBills
                    .Where(b => b.BillingMonth == month && b.BillingYear == year);

                totalBills = billsQuery.Count();
                paidCount = billsQuery.Count(b => b.PaymentStatus == "Paid");
                paidWithSurchargeCount = billsQuery.Count(b => b.PaymentStatus == "Paid with Surcharge");
                unpaidCount = billsQuery.Count(b => b.PaymentStatus == "Unpaid");

                // Count distinct customers
                totalCustomersCount = billsQuery
                    .Select(b => b.CustomerNo)
                    .Distinct()
                    .Count();

                paidCustomersCount = billsQuery
                    .Where(b => b.PaymentStatus == "Paid" || b.PaymentStatus == "Paid with Surcharge")
                    .Select(b => b.CustomerNo)
                    .Distinct()
                    .Count();

                unpaidCustomersCount = billsQuery
                    .Where(b => b.PaymentStatus == "Unpaid")
                    .Select(b => b.CustomerNo)
                    .Distinct()
                    .Count();

                // Calculate amounts
                generatedAmount = billsQuery.Sum(b => b.BillAmountInDueDate ?? 0);
                receivedAmount = billsQuery
                    .Where(b => b.PaymentStatus == "Paid" || b.PaymentStatus == "Paid with Surcharge")
                    .Sum(b => b.BillAmountInDueDate ?? 0);
                remainingAmount = billsQuery
                    .Where(b => b.PaymentStatus == "Unpaid")
                    .Sum(b => b.BillAmountInDueDate ?? 0);
            }
            else if (billType == "Maintenance Bills")
            {
                var billsQuery = _context.MaintenanceBills
                    .Where(b => b.BillingMonth == month && b.BillingYear == year);

                totalBills = billsQuery.Count();
                paidCount = billsQuery.Count(b => b.PaymentStatus == "Paid");
                paidWithSurchargeCount = billsQuery.Count(b => b.PaymentStatus == "Paid with Surcharge");
                unpaidCount = billsQuery.Count(b => b.PaymentStatus == "Unpaid");

                // Count distinct customers
                totalCustomersCount = billsQuery
                    .Select(b => b.CustomerNo)
                    .Distinct()
                    .Count();

                paidCustomersCount = billsQuery
                    .Where(b => b.PaymentStatus == "Paid" || b.PaymentStatus == "Paid with Surcharge")
                    .Select(b => b.CustomerNo)
                    .Distinct()
                    .Count();

                unpaidCustomersCount = billsQuery
                    .Where(b => b.PaymentStatus == "Unpaid")
                    .Select(b => b.CustomerNo)
                    .Distinct()
                    .Count();

                // Calculate amounts
                generatedAmount = billsQuery.Sum(b => b.BillAmountInDueDate ?? 0);
                receivedAmount = billsQuery
                    .Where(b => b.PaymentStatus == "Paid" || b.PaymentStatus == "Paid with Surcharge")
                    .Sum(b => b.BillAmountInDueDate ?? 0);
                remainingAmount = billsQuery
                    .Where(b => b.PaymentStatus == "Unpaid")
                    .Sum(b => b.BillAmountInDueDate ?? 0);
            }
            else
            {
                return Json(new
                {
                    Labels = new List<string> { "Paid", "Paid with Surcharge", "Unpaid" },
                    Data = new List<int> { 0, 0, 0 },
                    TotalBills = 0,
                    PaidCount = 0,
                    PaidWithSurchargeCount = 0,
                    UnpaidCount = 0,
                    CustomerLabels = new List<string> { "Total Customers", "Paid Customers", "Unpaid Customers" },
                    CustomerData = new List<int> { 0, 0, 0 },
                    TotalCustomersCount = 0,
                    PaidCustomersCount = 0,
                    UnpaidCustomersCount = 0,
                    AmountLabels = new List<string> { "Generated Amount", "Received Amount", "Remaining Amount" },
                    AmountData = new List<decimal> { 0, 0, 0 },
                    GeneratedAmount = 0,
                    ReceivedAmount = 0,
                    RemainingAmount = 0
                });
            }

            return Json(new
            {
                Labels = new List<string> { "Paid", "Paid with Surcharge", "Unpaid" },
                Data = new List<int> { paidCount, paidWithSurchargeCount, unpaidCount },
                TotalBills = totalBills,
                PaidCount = paidCount,
                PaidWithSurchargeCount = paidWithSurchargeCount,
                UnpaidCount = unpaidCount,
                CustomerLabels = new List<string> { "Total Customers", "Paid Customers", "Unpaid Customers" },
                CustomerData = new List<int> { totalCustomersCount, paidCustomersCount, unpaidCustomersCount },
                TotalCustomersCount = totalCustomersCount,
                PaidCustomersCount = paidCustomersCount,
                UnpaidCustomersCount = unpaidCustomersCount,
                AmountLabels = new List<string> { "Generated Amount", "Received Amount", "Remaining Amount" },
                AmountData = new List<decimal> { generatedAmount, receivedAmount, remainingAmount },
                GeneratedAmount = generatedAmount,
                ReceivedAmount = receivedAmount,
                RemainingAmount = remainingAmount
            });
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
