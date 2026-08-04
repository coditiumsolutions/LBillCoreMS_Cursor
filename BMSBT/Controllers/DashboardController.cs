using BMSBT.DTO;
using BMSBT.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.SqlClient;

namespace BMSBT.Controllers
{
    public class DashboardController : Controller
    {
       
            private readonly BmsbtContext _dbContext;
            private readonly ICurrentOperatorService _operatorService;
            public DashboardController(BmsbtContext dbContext, ICurrentOperatorService operatorService)
            {
                _dbContext = dbContext;
                _operatorService = operatorService;
            }


        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");
            base.OnActionExecuting(context);
        }

        public ActionResult EDashboard(string month, string year, string project)
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var model = new DashboardViewModel();

            string currentMonth = DateTime.Now.ToString("MMMM");
            string currentYear = DateTime.Now.Year.ToString();

            month = string.IsNullOrEmpty(month) ? currentMonth : month.Trim();
            year = string.IsNullOrEmpty(year) ? currentYear : year.Trim();
            project = string.IsNullOrWhiteSpace(project) ? null : project.Trim();

            try
            {
                model.Projects = _dbContext.CustomersDetails
                    .AsNoTracking()
                    .Where(c => c.Project != null && c.Project.Trim() != "")
                    .Select(c => c.Project.Trim())
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();

                model.Years = _dbContext.ElectricityBills
                    .AsNoTracking()
                    .Where(b => b.BillingYear != null && b.BillingYear.Trim() != "")
                    .Select(b => b.BillingYear!.Trim())
                    .Distinct()
                    .OrderByDescending(y => y)
                    .ToList();

                if (!model.Years.Contains(year))
                {
                    model.Years.Insert(0, year);
                }

                model.Months = new List<string>
                {
                    "January", "February", "March", "April", "May", "June",
                    "July", "August", "September", "October", "November", "December"
                };

                var billsQuery =
                    from bill in _dbContext.ElectricityBills.AsNoTracking()
                    join customer in _dbContext.CustomersDetails.AsNoTracking()
                        on bill.Btno equals customer.Btno
                    where bill.BillingMonth == month
                          && bill.BillingYear == year
                    select new { bill, customer };

                if (!string.IsNullOrWhiteSpace(project))
                {
                    billsQuery = billsQuery.Where(x =>
                        x.customer.Project != null &&
                        x.customer.Project.Trim() == project);
                }

                var periodList = billsQuery
                    .AsEnumerable()
                    .GroupBy(x => x.bill.Uid)
                    .Select(g => g.First())
                    .ToList();

                model.BscBillsExcluded = periodList.Count(x =>
                    (!string.IsNullOrEmpty(x.bill.Block) && x.bill.Block.ToUpper().Contains("BSC"))
                    || (!string.IsNullOrEmpty(x.customer.Block) && x.customer.Block.ToUpper().Contains("BSC"))
                    || (!string.IsNullOrEmpty(x.customer.Category) && x.customer.Category.ToUpper().Contains("BSC")));

                var mainBills = periodList
                    .Where(x =>
                        (x.bill.Block == null || !x.bill.Block.ToUpper().Contains("BSC")) &&
                        (x.customer.Block == null || !x.customer.Block.ToUpper().Contains("BSC")) &&
                        (x.customer.Category == null || !x.customer.Category.ToUpper().Contains("BSC")))
                    .ToList();

                static bool IsNetMeter(string? meterType) =>
                    !string.IsNullOrWhiteSpace(meterType) &&
                    meterType.Replace(" ", "", StringComparison.OrdinalIgnoreCase)
                             .Contains("netmeter", StringComparison.OrdinalIgnoreCase);

                static bool IsPaid(string? status)
                {
                    if (string.IsNullOrWhiteSpace(status)) return false;
                    var s = status.Trim().ToLowerInvariant();
                    return s == "paid"
                           || s == "paid with surcharge"
                           || s == "paidwithsurcharge";
                }

                static bool IsUnpaid(string? status)
                {
                    if (string.IsNullOrWhiteSpace(status)) return true;
                    var s = status.Trim().ToLowerInvariant();
                    return s == "unpaid"
                           || s == "partially paid"
                           || s == "paritally paid";
                }

                var standardBills = mainBills.Where(x =>
                    !IsNetMeter(x.bill.MeterType) && !IsNetMeter(x.customer.MeterType)).ToList();

                var netMeterBills = mainBills.Where(x =>
                    IsNetMeter(x.bill.MeterType) || IsNetMeter(x.customer.MeterType)).ToList();

                model.TotalBillsGenerated = standardBills.Count;
                model.TotalBillAmountGenerated = standardBills.Sum(x => x.bill.BillAmountInDueDate ?? x.bill.CurrentBill ?? 0);
                model.BillsUnits = (int)standardBills.Sum(x => x.bill.TotalUnit ?? x.bill.Difference1 ?? 0);

                model.NetMeterBillsGenerated = netMeterBills.Count;
                model.NetMeterTotalBilling = netMeterBills.Sum(x => x.bill.BillAmountInDueDate ?? x.bill.CurrentBill ?? 0);
                model.NetMeterBillsUnits = (int)netMeterBills.Sum(x => x.bill.TotalUnit ?? x.bill.Difference1 ?? 0);

                var paidBills = mainBills.Where(x => IsPaid(x.bill.PaymentStatus)).ToList();
                var unpaidBills = mainBills.Where(x => IsUnpaid(x.bill.PaymentStatus)).ToList();

                model.TotalBillsPaid = paidBills.Count;
                model.UnpaidBillsCount = unpaidBills.Count;
                model.TotalBillAmountCollected = paidBills.Sum(x =>
                    (x.bill.AmountPaid.HasValue && x.bill.AmountPaid.Value > 0)
                        ? x.bill.AmountPaid.Value
                        : (x.bill.BillAmountInDueDate ?? 0));
                model.BillUnpaidAmount = unpaidBills.Sum(x => x.bill.BillAmountInDueDate ?? x.bill.CurrentBill ?? 0);

                ViewBag.SelectedMonth = month;
                ViewBag.SelectedYear = year;
                ViewBag.SelectedProject = project;
                ViewBag.BillingPeriod = string.IsNullOrWhiteSpace(project)
                    ? $"{year} - {month} (All Projects)"
                    : $"{year} - {month} | {project}";

                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"An error occurred while loading the dashboard data: {ex.Message}");
                ViewBag.SelectedMonth = month;
                ViewBag.SelectedYear = year;
                ViewBag.SelectedProject = project;
                return View(new DashboardViewModel());
            }
        }




        //public ActionResult MDashboard(string month, string year, string project, string subProject, string sector, string block)
        //    {
        //        var model = new DashboardViewModel();

        //        // Get the current month and year
        //        string currentMonth = DateTime.Now.ToString("MMMM"); // Example: "March"
        //        string currentYear = DateTime.Now.Year.ToString();

        //        // Assign default values if parameters are null or empty
        //        month = string.IsNullOrEmpty(month) ? currentMonth : month;
        //        year = string.IsNullOrEmpty(year) ? currentYear : year;

        //        // Fetch dropdown data
        //        model.Projects = _dbContext.CustomersDetails
        //                            .Where(c => c.Project != null)
        //                            .Select(c => c.Project)
        //                            .Distinct()
        //                            .ToList();

        //        model.Sectors = _dbContext.CustomersDetails
        //                          .Where(c => c.Sector != null)
        //                          .Select(c => c.Sector)
        //                          .Distinct()
        //                          .ToList();

        //        model.Blocks = _dbContext.CustomersDetails
        //                         .Where(c => c.Block != null)
        //                         .Select(c => c.Block)
        //                         .Distinct()
        //                         .ToList();

        //        model.Years = _dbContext.ElectricityBills
        //                        .Where(e => e.BillingYear != null)
        //                        .Select(e => e.BillingYear)
        //                        .Distinct()
        //                        .ToList();

        //        model.Months = _dbContext.ElectricityBills
        //                         .Where(e => e.BillingMonth != null)
        //                         .Select(e => e.BillingMonth)
        //                         .Distinct()
        //                         .ToList();

        //        // Fetching dashboard statistics with filtering
        //        var query = from bill in _dbContext.MaintenanceBills
        //                    join customer in _dbContext.CustomersDetails on bill.Btno equals customer.Btno
        //                    select new { bill, customer };

        //        // Apply filters
        //        if (!string.IsNullOrEmpty(month))
        //        {
        //            query = query.Where(x => x.bill.BillingMonth == month);
        //        }

        //        if (!string.IsNullOrEmpty(year))
        //        {
        //            query = query.Where(x => x.bill.BillingYear == year);
        //        }

        //        if (!string.IsNullOrEmpty(project))
        //        {
        //            query = query.Where(x => x.customer.Project == project);
        //        }

        //        if (!string.IsNullOrEmpty(subProject))
        //        {
        //            query = query.Where(x => x.customer.SubProject == subProject);
        //        }

        //        if (!string.IsNullOrEmpty(sector))
        //        {
        //            query = query.Where(x => x.customer.Sector == sector);
        //        }

        //        if (!string.IsNullOrEmpty(block))
        //        {
        //            query = query.Where(x => x.customer.Block == block);
        //        }

        //        // Assign filtered data
        //        model.TotalBillsGenerated = query.Count();
        //        model.TotalBillsPaid = query.Count(x => x.bill.PaymentStatus == "Paid");
        //        model.TotalBillAmountGenerated = query.Sum(x => (decimal?)x.bill.BillAmountInDueDate) ?? 0;
        //        model.TotalBillAmountCollected = query.Where(x => x.bill.PaymentStatus == "Paid")
        //                                              .Sum(x => (decimal?)x.bill.BillAmountInDueDate) ?? 0;

        //        // Populate the table with filtered data
        //        model.BillingData = query.Select(x => new BillingDataViewModel
        //        {
        //            Project = x.customer.Project,
        //            SubProject = x.customer.SubProject,
        //            Sector = x.customer.Sector,
        //            Block = x.customer.Block,
        //            BillingMonth = x.bill.BillingMonth,
        //            TotalBillsGenerated = query.Count(y => y.customer.Project == x.customer.Project
        //                                                && y.customer.SubProject == x.customer.SubProject
        //                                                && y.customer.Sector == x.customer.Sector
        //                                                && y.customer.Block == x.customer.Block),
        //            TotalBillsPaid = query.Count(y => y.customer.Project == x.customer.Project
        //                                           && y.customer.SubProject == x.customer.SubProject
        //                                           && y.customer.Sector == x.customer.Sector
        //                                           && y.customer.Block == x.customer.Block
        //                                           && y.bill.PaymentStatus == "Paid"),
        //            TotalBillAmountGenerated = query.Where(y => y.customer.Project == x.customer.Project
        //                                                     && y.customer.SubProject == x.customer.SubProject
        //                                                     && y.customer.Sector == x.customer.Sector
        //                                                     && y.customer.Block == x.customer.Block)
        //                                            .Sum(y => (decimal?)y.bill.BillAmountInDueDate) ?? 0,
        //            TotalBillAmountCollected = query.Where(y => y.customer.Project == x.customer.Project
        //                                                     && y.customer.SubProject == x.customer.SubProject
        //                                                     && y.customer.Sector == x.customer.Sector
        //                                                     && y.customer.Block == x.customer.Block
        //                                                     && y.bill.PaymentStatus == "Paid")
        //                                            .Sum(y => (decimal?)y.bill.BillAmountInDueDate) ?? 0
        //        }).Distinct().ToList();

        //        // Store selected values in ViewBag to retain in UI
        //        ViewBag.SelectedMonth = month;
        //        ViewBag.SelectedYear = year;
        //        ViewBag.Projects = project;
        //        ViewBag.SubProjects = subProject;
        //        ViewBag.Sectors = sector;
        //        ViewBag.Blocks = block;

        //        return View(model);
        //    }

    }
    }