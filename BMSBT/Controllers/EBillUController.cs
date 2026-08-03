using BMSBT.BillServices;
using BMSBT.DTO;
using BMSBT.Models;
using BMSBT.Requests;
using BMSBT.Roles;
using BMSBT.Services;
using BMSBT.ViewModels;
using DevExpress.CodeParser;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using X.PagedList.Extensions;
using X.PagedList;
using static BMSBT.Controllers.MaintenanceBillController;

namespace BMSBT.Controllers
{
   
    public class EBillUController : Controller
    {
        private readonly BmsbtContext _dbContext;
        private readonly ElectrcityFunctions ElectrcityFunctions;
        private readonly ICurrentOperatorService _operatorService;
        private readonly IHttpClientFactory _httpClientFactory;

        public EBillUController(IHttpClientFactory httpClientFactory, BmsbtContext dbContext, ICurrentOperatorService operatorService)
        {
            _dbContext = dbContext;
            ElectrcityFunctions = new ElectrcityFunctions(_dbContext);
            _operatorService = operatorService;
            _httpClientFactory = httpClientFactory;
        }

        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");
            base.OnActionExecuting(context);
        }





        // Electricity billing summary — data from ElectricityBills + CustomersDetail
        public IActionResult Index(string month, string year, string project)
        {
            var model = new DTO.DashboardViewModel();

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

                // Join bills to customers for project filter (ElectricityBills has no Project column)
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

                // Materialize period bills (dedupe by bill uid)
                var periodList = billsQuery
                    .AsEnumerable()
                    .GroupBy(x => x.bill.Uid)
                    .Select(g => g.First())
                    .ToList();

                model.BscBillsExcluded = periodList.Count(x =>
                    (!string.IsNullOrEmpty(x.bill.Block) && x.bill.Block.ToUpper().Contains("BSC"))
                    || (!string.IsNullOrEmpty(x.customer.Block) && x.customer.Block.ToUpper().Contains("BSC"))
                    || (!string.IsNullOrEmpty(x.customer.Category) && x.customer.Category.ToUpper().Contains("BSC")));

                // BSC excluded from main generated totals
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
                return View(new DTO.DashboardViewModel());
            }
        }






        public IActionResult GenerateBillMain(string project, string sector, string block, int? page)
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.Username = HttpContext.Session.GetString("UserName");
            ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");

            // Populate dropdown data
            ViewBag.Projects = _dbContext.Configurations
                                 .Where(c => c.ConfigKey == "Project")
                                 .Select(c => c.ConfigValue)
                                 .ToList();


            var Sectors = _dbContext.Configurations
                                   .Where(c => c.ConfigKey == project)
                                   .Select(c => c.ConfigValue)
                                   .ToList();
            ViewBag.Sectors = Sectors;

            // Get all sectors (assuming the field is "Sector" in your database)
            ViewBag.Blocks = _dbContext.Configurations
                                  .Where(c => c.ConfigKey == "Block" + project)
                                  .Select(c => c.ConfigValue)
                                  .ToList();

            ViewBag.Tarrif = _dbContext.Tarrifs.Select(t => new { t.Uid, t.TarrifName }).ToList();

            // Apply filters
            var query = _dbContext.CustomersDetails.AsQueryable();

            if (!string.IsNullOrEmpty(project))
                query = query.Where(x => x.Project == project);

            if (!string.IsNullOrEmpty(sector))
                query = query.Where(x => x.Sector == sector);

            if (!string.IsNullOrEmpty(block))
                query = query.Where(x => x.Block == block);

            // Total Records Count
            ViewBag.TotalRecords = query.Count();
            // Calculate total records by category
            ViewBag.TotalRecordsByProject = _dbContext.CustomersDetails.Count(x => x.Project == project);
            ViewBag.TotalRecordsBySector = _dbContext.CustomersDetails.Count(x => x.Sector == sector);
            ViewBag.TotalRecordsByBlock = _dbContext.CustomersDetails.Count(x => x.Block == block);

            int pageNumber = page ?? 1;
            int pageSize = 5000;

            return View(query.ToPagedList(pageNumber, pageSize));
        }











        public IActionResult CreateCustomer()
        {
            // Return an empty view (or you could pass an empty IPagedList<BillDTO> if needed)
            return View();
        }


       
        [HttpPost]
        public IActionResult CreateCustomer(CustomersDetail cust)
        {
            _dbContext.CustomersDetails.Add(cust);
            _dbContext.SaveChanges();
            return View();
        }


       
        [HttpPost]
        public IActionResult EditCustomer(CustomersDetail model)
        {
            if (model == null)
            {
                return BadRequest("Invalid customer data.");
            }

            var existingCustomer = _dbContext.CustomersDetails.FirstOrDefault(c => c.Btno == model.Btno);
            if (existingCustomer == null)
            {
                return NotFound();
            }

            // Update customer properties
            existingCustomer.CustomerName = model.CustomerName;
            existingCustomer.MobileNo = model.MobileNo;
            existingCustomer.TelephoneNo = model.TelephoneNo;
            existingCustomer.BankNo = model.BankNo;
            existingCustomer.City = model.City;
            existingCustomer.SubProject = model.SubProject;
            existingCustomer.Project = model.Project;
            existingCustomer.Size = model.Size;
            existingCustomer.Block = model.Block;
            existingCustomer.Cnicno = model.Cnicno;
            existingCustomer.City = model.City;
            existingCustomer.Project = model.Project;
            existingCustomer.SubProject = model.SubProject;
            existingCustomer.TariffName = model.TariffName;
            existingCustomer.Sector = model.Sector;
            existingCustomer.Block = model.Block;
            existingCustomer.PloNo = model.PloNo;
            existingCustomer.PlotType = model.PlotType;
            existingCustomer.BtnoMaintenance = model.BtnoMaintenance;
            existingCustomer.Category = model.Category;
            existingCustomer.Ntnnumber = model.Ntnnumber;
            existingCustomer.BankNo = model.BankNo;
            existingCustomer.InstalledOn = model.InstalledOn;
            existingCustomer.FatherName = model.FatherName;
            try
            {
                _dbContext.SaveChanges();
                return RedirectToAction("Index"); // Redirect to customer list
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating the customer.");
            }

            return View(model);
        }


        
        [HttpGet]
        public IActionResult EditCustomer(string id)
        {
            // Retrieve the customer using Btno
            var customer = _dbContext.CustomersDetails.FirstOrDefault(c => c.Btno == id);
            if (customer == null)
            {
                return NotFound();
            }

            // Retrieve all billing records for the customer (using Btno)
            var bills = _dbContext.ElectricityBills
                          .Where(b => b.Btno == id)
                          .ToList();

            // Map customer and billing details to the composite view model
            var viewModel = new CustomerBillingViewModel
            {
                Uid = customer.Uid,
                CustomerNo = customer.CustomerNo,
                Btno = customer.Btno,
                CustomerName = customer.CustomerName,
                GeneratedMonthYear = customer.GeneratedMonthYear,
                LocationSeqNo = customer.LocationSeqNo,
                Cnicno = customer.Cnicno,
                FatherName = customer.FatherName,
                InstalledOn = customer.InstalledOn,
                MobileNo = customer.MobileNo,
                TelephoneNo = customer.TelephoneNo,
                MeterType = customer.MeterType,
                Ntnnumber = customer.Ntnnumber,
                City = customer.City,
                Project = customer.Project,
                SubProject = customer.SubProject,
                TariffName = customer.TariffName,
                BankNo = customer.BankNo,
                BtnoMaintenance = customer.BtnoMaintenance,
                Category = customer.Category,
                Block = customer.Block,
                PlotType = customer.PlotType,
                Size = customer.Size,
                Sector = customer.Sector,
                PloNo = customer.PloNo,
                Bills = bills
            };

            return View(viewModel);
        }



       


        public IActionResult GenerateEBill(string project, string sector, string block, int? page)
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.Username = HttpContext.Session.GetString("UserName");
            ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");

            // Populate dropdown data
            ViewBag.Projects = _dbContext.Configurations
                                 .Where(c => c.ConfigKey == "Project")
                                 .Select(c => c.ConfigValue)
                                 .ToList();


            var Sectors = _dbContext.Configurations
                                   .Where(c => c.ConfigKey == project)
                                   .Select(c => c.ConfigValue)
                                   .ToList();
            ViewBag.Sectors = Sectors;

            // Get all sectors (assuming the field is "Sector" in your database)
            ViewBag.Blocks = _dbContext.Configurations
                                  .Where(c => c.ConfigKey == "Block" + project)
                                  .Select(c => c.ConfigValue)
                                  .ToList();

            ViewBag.Tarrif = _dbContext.Tarrifs.Select(t => new { t.Uid, t.TarrifName }).ToList();

            // Apply filters
            var query = _dbContext.CustomersDetails.AsQueryable();

            if (!string.IsNullOrEmpty(project))
                query = query.Where(x => x.Project == project);

            if (!string.IsNullOrEmpty(sector))
                query = query.Where(x => x.Sector == sector);

            if (!string.IsNullOrEmpty(block))
                query = query.Where(x => x.Block == block);

            // Total Records Count
            ViewBag.TotalRecords = query.Count();
            // Calculate total records by category
            ViewBag.TotalRecordsByProject = _dbContext.CustomersDetails.Count(x => x.Project == project);
            ViewBag.TotalRecordsBySector = _dbContext.CustomersDetails.Count(x => x.Sector == sector);
            ViewBag.TotalRecordsByBlock = _dbContext.CustomersDetails.Count(x => x.Block == block);

            int pageNumber = page ?? 1;
            int pageSize = 5000;

            return View(query.ToPagedList(pageNumber, pageSize));
        }




        // AJAX endpoint for cascading dropdowns
        public JsonResult GetSubprojects(string project)
        {
            if (string.IsNullOrEmpty(project))
                return Json(new List<string>());


            var SubProjects = _dbContext.Configurations
                                 .Where(c => c.ConfigKey == project)
                                 .Select(c => c.ConfigValue)
                                 .ToList();
            return Json(SubProjects);
        }


        [HttpGet]
        public IActionResult EBill()
        {
            var projects = _dbContext.Configurations
                           .Where(c => c.ConfigKey == "Project")
                           .Select(c => c.ConfigValue)
                           .ToList();

            ViewBag.Projects = projects;
            // Return an empty view (or you could pass an empty IPagedList<BillDTO> if needed)
            return View();
        }



        public IActionResult EBill(string? month, string? year, string Category, string Block)
        {

            if (string.IsNullOrEmpty(month) || string.IsNullOrEmpty(year) && string.IsNullOrEmpty(Category) && string.IsNullOrEmpty(Block))
            {
                ViewBag.ErrorMessage = "Both month and year must be selected.";
                return View("MaintenanceBills"); // Return the view with an error message
            }


            // Query electricity bills joining ElectricityBills and CustomersDetails
            var bills = (
                from bill in _dbContext.ElectricityBills
                join customer in _dbContext.CustomersDetails
                     on bill.Btno equals customer.Btno
                where bill.BillingMonth == month && bill.BillingYear == year
                select new BillDTO
                {
                    Uid = bill.Uid,
                    CustomerNo = customer.CustomerNo,
                    Btno = bill.Btno,
                    CustomerName = customer.CustomerName,
                    Cnicno = customer.Cnicno,
                    FatherName = customer.FatherName,
                    InstalledOn = customer.InstalledOn,
                    MobileNo = customer.MobileNo,
                    TelephoneNo = customer.TelephoneNo,
                    Ntnnumber = customer.Ntnnumber,
                    City = customer.City,
                    Project = customer.Project,
                    SubProject = customer.SubProject,
                    TariffName = customer.TariffName,
                    BankNo = customer.BankNo,
                    BtnoMaintenance = customer.BtnoMaintenance,
                    Category = customer.Category,
                    Block = customer.Block,
                    PlotType = customer.PlotType,
                    Size = customer.Size,
                    Sector = customer.Sector,
                    PloNo = customer.PloNo,
                    BillStatusMaint = customer.BillStatusMaint,
                    BillStatus = customer.BillStatus,
                    InvoiceNo = bill.InvoiceNo,
                    BillingMonth = bill.BillingMonth,
                    BillingYear = bill.BillingYear,
                    BillingDate = bill.BillingDate,
                    DueDate = bill.DueDate,
                    IssueDate = bill.IssueDate,
                    ValidDate = bill.ValidDate,
                    PaymentStatus = bill.PaymentStatus,
                    PaymentDate = bill.PaymentDate,
                    PaymentMethod = bill.PaymentMethod,
                    BankDetail = bill.BankDetail,
                    

                    BillAmountInDueDate = bill.BillAmountInDueDate,
                    BillSurcharge = bill.BillSurcharge,
                    BillAmountAfterDueDate = bill.BillAmountAfterDueDate
                }
            ).ToList();

            if (!bills.Any())
            {
                ViewBag.ErrorMessage = "No bills found for the selected month and year.";
            }

            // Optionally, convert to a paged list (adjust page number and page size as needed)
            var pagedBills = bills.ToPagedList(1, 5000);

            return View("EBills", pagedBills); // Pass the view model to the view
        }










        [HttpGet]
        public IActionResult EBillAMI()
        {
            var projects = _dbContext.Configurations
                           .Where(c => c.ConfigKey == "Project")
                           .Select(c => c.ConfigValue)
                           .ToList();

            ViewBag.Projects = projects;
            return View();
        }


        [HttpGet]
        public IActionResult EBillNetMeter()
        {
            var projects = _dbContext.Configurations
                           .Where(c => c.ConfigKey == "Project")
                           .Select(c => c.ConfigValue)
                           .ToList();

            ViewBag.Projects = projects;
            // Return an empty view (or you could pass an empty IPagedList<BillDTO> if needed)
            return View();
        }



        public IActionResult EBillNetMeter(string? month, string? year, string Category, string Block)
        {

            if (string.IsNullOrEmpty(month) || string.IsNullOrEmpty(year) && string.IsNullOrEmpty(Category) && string.IsNullOrEmpty(Block))
            {
                ViewBag.ErrorMessage = "Both month and year must be selected.";
                return View("MaintenanceBills"); // Return the view with an error message
            }


            // Query electricity bills joining ElectricityBills and CustomersDetails
            var bills = (
                from bill in _dbContext.ElectricityBills
                join customer in _dbContext.CustomersDetails
                     on bill.Btno equals customer.Btno
                where bill.BillingMonth == month && bill.BillingYear == year
                select new BillDTO
                {
                    Uid = bill.Uid,
                    CustomerNo = customer.CustomerNo,
                    Btno = bill.Btno,
                    CustomerName = customer.CustomerName,
                    Cnicno = customer.Cnicno,
                    FatherName = customer.FatherName,
                    InstalledOn = customer.InstalledOn,
                    MobileNo = customer.MobileNo,
                    TelephoneNo = customer.TelephoneNo,
                    Ntnnumber = customer.Ntnnumber,
                    City = customer.City,
                    Project = customer.Project,
                    SubProject = customer.SubProject,
                    TariffName = customer.TariffName,
                    BankNo = customer.BankNo,
                    BtnoMaintenance = customer.BtnoMaintenance,
                    Category = customer.Category,
                    Block = customer.Block,
                    PlotType = customer.PlotType,
                    Size = customer.Size,
                    Sector = customer.Sector,
                    PloNo = customer.PloNo,
                    BillStatusMaint = customer.BillStatusMaint,
                    BillStatus = customer.BillStatus,
                    InvoiceNo = bill.InvoiceNo,
                    BillingMonth = bill.BillingMonth,
                    BillingYear = bill.BillingYear,
                    BillingDate = bill.BillingDate,
                    DueDate = bill.DueDate,
                    IssueDate = bill.IssueDate,
                    ValidDate = bill.ValidDate,
                    PaymentStatus = bill.PaymentStatus,
                    PaymentDate = bill.PaymentDate,
                    PaymentMethod = bill.PaymentMethod,
                    BankDetail = bill.BankDetail,


                    BillAmountInDueDate = bill.BillAmountInDueDate,
                    BillSurcharge = bill.BillSurcharge,
                    BillAmountAfterDueDate = bill.BillAmountAfterDueDate
                }
            ).ToList();

            if (!bills.Any())
            {
                ViewBag.ErrorMessage = "No bills found for the selected month and year.";
            }

            // Optionally, convert to a paged list (adjust page number and page size as needed)
            var pagedBills = bills.ToPagedList(1, 5000);

            return View("EBills", pagedBills); // Pass the view model to the view
        }













        [HttpGet]
        public IActionResult EBills()
        {
            // Return an empty view (or you could pass an empty IPagedList<BillDTO> if needed)
            return View();
        }
        public IActionResult EBillsPost(string? month, string? year)
        {

            if (string.IsNullOrEmpty(month) || string.IsNullOrEmpty(year))
            {
                ViewBag.ErrorMessage = "Both month and year must be selected.";
                return View("MaintenanceBills"); // Return the view with an error message
            }


            // Query electricity bills joining ElectricityBills and CustomersDetails
            var bills = (
                from bill in _dbContext.ElectricityBills
                join customer in _dbContext.CustomersDetails
                     on bill.Btno equals customer.Btno
                where bill.BillingMonth == month && bill.BillingYear == year && bill.Block.Contains("COM")
                select new BillDTO
                {
                    Uid = bill.Uid,
                    CustomerNo = customer.CustomerNo,
                    Btno = bill.Btno,
                    CustomerName = customer.CustomerName,
                    Cnicno = customer.Cnicno,
                    FatherName = customer.FatherName,
                    InstalledOn = customer.InstalledOn,
                    MobileNo = customer.MobileNo,
                    TelephoneNo = customer.TelephoneNo,
                    Ntnnumber = customer.Ntnnumber,
                    City = customer.City,
                    Project = customer.Project,
                    SubProject = customer.SubProject,
                    TariffName = customer.TariffName,
                    BankNo = customer.BankNo,
                    BtnoMaintenance = customer.BtnoMaintenance,
                    Category = customer.Category,
                    Block = customer.Block,
                    PlotType = customer.PlotType,
                    Size = customer.Size,
                    Sector = customer.Sector,
                    PloNo = customer.PloNo,
                    BillStatusMaint = customer.BillStatusMaint,
                    BillStatus = customer.BillStatus,
                    InvoiceNo = bill.InvoiceNo,
                    BillingMonth = bill.BillingMonth,
                    BillingYear = bill.BillingYear,
                    BillingDate = bill.BillingDate,
                    DueDate = bill.DueDate,
                    IssueDate = bill.IssueDate,
                    ValidDate = bill.ValidDate,
                    PaymentStatus = bill.PaymentStatus,
                    PaymentDate = bill.PaymentDate,
                    PaymentMethod = bill.PaymentMethod,
                    BankDetail = bill.BankDetail,
               

                    BillAmountInDueDate = bill.BillAmountInDueDate,
                    BillSurcharge = bill.BillSurcharge,
                    BillAmountAfterDueDate = bill.BillAmountAfterDueDate
                }
            ).ToList();
            bills = bills.OrderBy(x => NaturalSortKey(x.PloNo)).ToList();
            if (!bills.Any())
            {
                ViewBag.ErrorMessage = "No bills found for the selected month and year.";
            }

            // Optionally, convert to a paged list (adjust page number and page size as needed)
            var pagedBills = bills.ToPagedList(1, 5000);

            return View("EBills", pagedBills); // Pass the view model to the view
        }


       
        //[HttpGet]
        //[Route("PrintBills")]
        // GET: Reading/SearchPrintBills
        public IActionResult SearchPrintBills(string selectedMonth, string selectedYear, string selectedSector, string btnoSearch)
        {
            var model = new BillSearchViewModel
            {
                Months = new List<string>
            {
                "January", "February", "March", "April", "May", "June",
                "July", "August", "September", "October", "November", "December"
            },
                Years = Enumerable.Range(DateTime.Now.Year - 5, 6).Select(y => y.ToString()).ToList(),
                Sectors = _dbContext.ElectricityBills.Select(b => b.Sector).Distinct().OrderBy(s => s).ToList()
            };

            if (!string.IsNullOrEmpty(btnoSearch))
            {
                // Priority: Search by BTNo
                model.Results = _dbContext.ElectricityBills
                                        .Where(b => b.Btno == btnoSearch)
                                        .ToList();
            }
            else if (!string.IsNullOrEmpty(selectedMonth) &&
                     !string.IsNullOrEmpty(selectedYear) &&
                     !string.IsNullOrEmpty(selectedSector))
            {
                // Search by Month + Year + Sector
                model.Results = _dbContext.ElectricityBills
                                        .Where(b => b.BillingMonth == selectedMonth &&
                                                    b.BillingYear == selectedYear &&
                                                    b.Sector == selectedSector)
                                        .ToList();
            }

            model.SelectedMonth = selectedMonth;
            model.SelectedYear = selectedYear;
            model.SelectedSector = selectedSector;
            model.BtnoSearch = btnoSearch;

            return View(model);
        }


        // GET: EBillU/PrintView/5
        [HttpGet]
        public IActionResult PrintView(int id)
        {
            var bill = _dbContext.ElectricityBills.Find(id); // efficient for primary key
            if (bill == null)
            {
                return NotFound();
            }

            return View("PrintView", bill);
        }











        [HttpGet]
        [Route("GetSectorsAndBlocks")]
        public IActionResult GetSectorsAndBlocks(string project)
        {
            if (string.IsNullOrEmpty(project))
            {
                return BadRequest("Project is required.");
            }

            var sectors = _dbContext.Configurations
                              .Where(c => c.ConfigKey == project)
                              .Select(c => c.ConfigValue)
                              .ToList();

            var blocks = _dbContext.Configurations
                             .Where(c => c.ConfigKey == "Block" + project)
                             .Select(c => c.ConfigValue)
                             .ToList();

            return Json(new { sectors, blocks });
        }

        //public IActionResult EBillsPost(string? month, string? year,string Sector,string Block)
        //{

        //    if (string.IsNullOrEmpty(month) || string.IsNullOrEmpty(year) && string.IsNullOrEmpty(Sector) && string.IsNullOrEmpty(Block))
        //    {
        //        ViewBag.ErrorMessage = "Both month and year must be selected.";
        //        return View("MaintenanceBills"); // Return the view with an error message
        //    }


        //    // Query electricity bills joining ElectricityBills and CustomersDetails
        //    var bills = (
        //        from bill in _dbContext.ElectricityBills
        //        join customer in _dbContext.CustomersDetails
        //             on bill.Btno equals customer.Btno
        //        where bill.BillingMonth == month && bill.BillingYear == year
        //        select new BillDTO
        //        {
        //            Uid = bill.Uid,
        //            CustomerNo = customer.CustomerNo,
        //            Btno = bill.Btno,
        //            CustomerName = customer.CustomerName,
        //            Cnicno = customer.Cnicno,
        //            FatherName = customer.FatherName,
        //            InstalledOn = customer.InstalledOn,
        //            MobileNo = customer.MobileNo,
        //            TelephoneNo = customer.TelephoneNo,
        //            Ntnnumber = customer.Ntnnumber,
        //            City = customer.City,
        //            Project = customer.Project,
        //            SubProject = customer.SubProject,
        //            TariffName = customer.TariffName,
        //            BankNo = customer.BankNo,
        //            BtnoMaintenance = customer.BtnoMaintenance,
        //            Category = customer.Category,
        //            Block = customer.Block,
        //            PlotType = customer.PlotType,
        //            Size = customer.Size,
        //            Sector = customer.Sector,
        //            PloNo = customer.PloNo,
        //            BillStatusMaint = customer.BillStatusMaint,
        //            BillStatus = customer.BillStatus,
        //            InvoiceNo = bill.InvoiceNo,
        //            BillingMonth = bill.BillingMonth,
        //            BillingYear = bill.BillingYear,
        //            BillingDate = bill.BillingDate,
        //            DueDate = bill.DueDate,
        //            IssueDate = bill.IssueDate,
        //            ValidDate = bill.ValidDate,
        //            PaymentStatus = bill.PaymentStatus,
        //            PaymentDate = bill.PaymentDate,
        //            PaymentMethod = bill.PaymentMethod,
        //            BankDetail = bill.BankDetail,
        //            LastUpdated = bill.LastUpdated,

        //            BillAmountInDueDate = bill.BillAmountInDueDate,
        //            BillSurcharge = bill.BillSurcharge,
        //            BillAmountAfterDueDate = bill.BillAmountAfterDueDate
        //        }
        //    ).ToList();

        //    if (!bills.Any())
        //    {
        //        ViewBag.ErrorMessage = "No bills found for the selected month and year.";
        //    }

        //    // Optionally, convert to a paged list (adjust page number and page size as needed)
        //    var pagedBills = bills.ToPagedList(1, 5000);

        //    return View("EBills", pagedBills); // Pass the view model to the view
        //}




        [HttpPost]
        [Route("GenerateElectricityBills")]
        public async Task<IActionResult> GenerateElectricityBills([FromBody] ElectricityBillRequest request)
        {
            string operatorId = HttpContext.Session.GetString("OperatorId");
            string userName = HttpContext.Session.GetString("UserName");
            ViewBag.Username = userName;

            // Align with Operator Setup for this login (OperatorName), not stale EmployeeId mapping
            var resolvedSetup = OperatorSetupResolver.Resolve(_dbContext, userName, operatorId);
            if (resolvedSetup != null && !string.IsNullOrWhiteSpace(resolvedSetup.OperatorID))
            {
                operatorId = resolvedSetup.OperatorID;
                HttpContext.Session.SetString("OperatorId", operatorId);
            }

            if (string.IsNullOrEmpty(operatorId))
            {
                return new JsonResult(new { success = false, message = "Operator ID not found in session" });
            }

            await _operatorService.InitializeAsync(operatorId);
            var currentOperator = _operatorService.GetCurrentOperator();

            if (currentOperator == null)
            {
                return new JsonResult(new { success = false, message = "Operator details not found" });
            }

            if (string.IsNullOrEmpty(currentOperator.BillingMonth) || string.IsNullOrEmpty(currentOperator.BillingYear))
            {
                return new JsonResult(new { success = false, message = "Please Update Operator Setup" });
            }

            string billingMonth = currentOperator.BillingMonth;
            string billingYear = currentOperator.BillingYear.ToString();

            if (string.IsNullOrEmpty(billingMonth) || string.IsNullOrEmpty(billingYear))
            {
                return new JsonResult(new { success = false, message = "Month and Year must be provided." });
            }

            if (request?.SelectedIds == null || request.SelectedIds.Count == 0)
            {
                return new JsonResult(new { success = false, message = "No customers selected." });
            }

            ElectrcityFunctions.GetPreviousBillingPeriod(billingMonth, billingYear);
            string previousMonth = BillCreationState.PreviousMonth;
            string previousYear = BillCreationState.PreviousYear;

            DateOnly? IssueDate = currentOperator.IssueDate.HasValue
                ? DateOnly.FromDateTime(currentOperator.IssueDate.Value)
                : (DateOnly?)null;

            DateOnly? DueDate = currentOperator.DueDate.HasValue
                ? DateOnly.FromDateTime(currentOperator.DueDate.Value)
                : (DateOnly?)null;

            DateOnly? ValidDate = currentOperator.ValidDate;
            string FPAMONTH1 = currentOperator.FPAMonth1;
            string FPAYEAR1 = currentOperator.FPAYEAR1;
            decimal? FPARATE1 = currentOperator.FPARate1;

            string FPAMONTH2 = currentOperator.FPAMonth2;
            string FPAYEAR2 = currentOperator.FPAYEAR2;
            decimal? FPARATE2 = currentOperator.FPARate2;

            var successResults = new List<string>();
            var failureResults = new List<string>();

            foreach (var customerId in request.SelectedIds)
            {
                try
                {
                    var result = ElectrcityFunctions.GenerateEBillForCustomer(
                        customerId, billingMonth, billingYear, previousMonth, previousYear,
                        IssueDate, DueDate, ValidDate, ViewBag.UserName,
                        FPAMONTH1, FPAYEAR1, FPARATE1, FPAMONTH2, FPAYEAR2, FPARATE2);

                    if (!string.IsNullOrWhiteSpace(result) &&
                        result.StartsWith("Bill created successfully", StringComparison.OrdinalIgnoreCase))
                    {
                        successResults.Add(result);
                    }
                    else
                    {
                        failureResults.Add(string.IsNullOrWhiteSpace(result) ? $"Customer ID {customerId}: Unknown error." : result);
                    }
                }
                catch (Exception ex)
                {
                    var detail = ex.Message;
                    var inner = ex.InnerException;
                    while (inner != null)
                    {
                        detail += " | " + inner.Message;
                        inner = inner.InnerException;
                    }
                    failureResults.Add($"Customer ID {customerId}: {detail}");
                }
            }

            if (successResults.Count > 0)
            {
                var message = successResults.Count == 1
                    ? "Bills Generated"
                    : $"Bills Generated ({successResults.Count})";

                if (failureResults.Count > 0)
                {
                    message += "\n\nSome bills were not generated:\n" + string.Join("\n", failureResults);
                }

                return new JsonResult(new
                {
                    success = true,
                    generatedCount = successResults.Count,
                    failedCount = failureResults.Count,
                    message,
                    results = successResults,
                    failures = failureResults
                });
            }

            return new JsonResult(new
            {
                success = false,
                generatedCount = 0,
                failedCount = failureResults.Count,
                message = "No bills were generated.\n\n" + string.Join("\n", failureResults),
                results = successResults,
                failures = failureResults
            });
        }











        [Route("PrintEMultiBill")]
        [HttpPost]
        public async Task<IActionResult> PrintEMultiBill([FromBody] PrintBillRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.uids))
                {
                    return BadRequest("Please select at least one bill to print.");
                }

                var uidList = request.uids
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(x => int.TryParse(x, out _))
                    .Select(int.Parse)
                    .Distinct()
                    .ToList();

                if (uidList.Count == 0)
                {
                    return BadRequest("Please select at least one bill to print.");
                }

                var selectedSet = uidList.ToHashSet();

                // Load selected bills with customer filters
                var selectedBills = (
                    from bill in _dbContext.ElectricityBills
                    join customer in _dbContext.CustomersDetails on bill.Btno equals customer.Btno
                    where selectedSet.Contains(bill.Uid)
                    select new
                    {
                        bill.Uid,
                        bill.Btno,
                        bill.BillingMonth,
                        bill.BillingYear,
                        Category = customer.Category,
                        Block = customer.Block,
                        Project = customer.Project
                    }
                ).ToList();

                if (selectedBills.Count == 0)
                {
                    return BadRequest("Selected bill records were not found.");
                }

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(90);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));

                // Try UID API first (exact selection) — may be unavailable
                var uidsParam = string.Join(",", uidList);
                foreach (var uidUrl in new[]
                {
                    $"http://172.20.229.3:84/api/ElectricityBill/GetEBillByUid?uids={uidsParam}",
                    $"http://172.20.228.2:81/api/ElectricityBill/GetEBillByUid?uids={uidsParam}"
                })
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                        var uidResponse = await client.GetAsync(uidUrl, cts.Token);
                        if (uidResponse.IsSuccessStatusCode)
                        {
                            var pdf = await uidResponse.Content.ReadAsByteArrayAsync(cts.Token);
                            if (pdf is { Length: > 0 })
                            {
                                Response.Headers["Content-Disposition"] = "attachment; filename=ElectricityBills.pdf";
                                return File(pdf, "application/pdf");
                            }
                        }
                    }
                    catch
                    {
                        // fall through to page-filter approach
                    }
                }

                // Working API returns 1 PDF page per bill for a Category/Block/Month/Year/Project set.
                // Download each set PDF and keep only pages that map to the checked UIDs.
                using var outputDoc = new PdfSharpCore.Pdf.PdfDocument();
                var billGroups = selectedBills
                    .GroupBy(b => new
                    {
                        Category = (b.Category ?? "").Trim(),
                        Block = (b.Block ?? "").Trim(),
                        Month = (b.BillingMonth ?? "").Trim(),
                        Year = (b.BillingYear ?? "").Trim(),
                        Project = (b.Project ?? "").Trim()
                    })
                    .ToList();

                foreach (var billGroup in billGroups)
                {
                    var category = billGroup.Key.Category;
                    var block = billGroup.Key.Block;
                    var month = billGroup.Key.Month;
                    var year = billGroup.Key.Year;
                    var project = billGroup.Key.Project;

                    if (string.IsNullOrWhiteSpace(category)
                        || string.IsNullOrWhiteSpace(block)
                        || string.IsNullOrWhiteSpace(month)
                        || string.IsNullOrWhiteSpace(year))
                    {
                        return BadRequest("Selected bills are missing Category/Block/Month/Year required for printing.");
                    }

                    var groupBills = _dbContext.ElectricityBills
                        .Join(
                            _dbContext.CustomersDetails,
                            bill => bill.Btno,
                            customer => customer.Btno,
                            (bill, customer) => new { bill, customer })
                        .Where(x =>
                            x.bill.BillingMonth == month
                            && x.bill.BillingYear == year
                            && x.customer.Category != null
                            && x.customer.Category.Trim() == category
                            && x.customer.Block != null
                            && x.customer.Block.Trim() == block
                            && (project == ""
                                || (x.customer.Project != null && x.customer.Project.Trim() == project)))
                        .OrderBy(x => x.bill.Btno)
                        .Select(x => x.bill.Uid)
                        .ToList();

                    var url =
                        "http://172.20.228.2:81/api/ElectricityBill/GetEBill" +
                        $"?category={Uri.EscapeDataString(category)}" +
                        $"&block={Uri.EscapeDataString(block)}" +
                        $"&month={Uri.EscapeDataString(month)}" +
                        $"&year={Uri.EscapeDataString(year)}" +
                        $"&project={Uri.EscapeDataString(project)}";

                    var response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        return StatusCode((int)response.StatusCode, $"API Error: {errorContent}");
                    }

                    var groupPdf = await response.Content.ReadAsByteArrayAsync();
                    if (groupPdf == null || groupPdf.Length == 0)
                    {
                        return BadRequest("Received empty PDF data from print service.");
                    }

                    using var inputStream = new MemoryStream(groupPdf);
                    using var inputDoc = PdfSharpCore.Pdf.IO.PdfReader.Open(
                        inputStream,
                        PdfSharpCore.Pdf.IO.PdfDocumentOpenMode.Import);

                    // Expect 1 page per bill in BTNo order
                    if (inputDoc.PageCount != groupBills.Count)
                    {
                        // Retry with Uid order mapping if counts still match
                        groupBills = _dbContext.ElectricityBills
                            .Join(
                                _dbContext.CustomersDetails,
                                bill => bill.Btno,
                                customer => customer.Btno,
                                (bill, customer) => new { bill, customer })
                            .Where(x =>
                                x.bill.BillingMonth == month
                                && x.bill.BillingYear == year
                                && x.customer.Category != null
                                && x.customer.Category.Trim() == category
                                && x.customer.Block != null
                                && x.customer.Block.Trim() == block
                                && (project == ""
                                    || (x.customer.Project != null && x.customer.Project.Trim() == project)))
                            .OrderBy(x => x.bill.Uid)
                            .Select(x => x.bill.Uid)
                            .ToList();

                        if (inputDoc.PageCount != groupBills.Count)
                        {
                            return BadRequest(
                                $"Cannot map selected bills to PDF pages (pages={inputDoc.PageCount}, bills={groupBills.Count}). " +
                                "UID print service is unavailable.");
                        }
                    }

                    for (int i = 0; i < inputDoc.PageCount; i++)
                    {
                        if (selectedSet.Contains(groupBills[i]))
                        {
                            outputDoc.AddPage(inputDoc.Pages[i]);
                        }
                    }
                }

                if (outputDoc.PageCount == 0)
                {
                    return BadRequest("No PDF pages matched the selected bills.");
                }

                using var outStream = new MemoryStream();
                outputDoc.Save(outStream, false);
                Response.Headers["Content-Disposition"] = "attachment; filename=ElectricityBills.pdf";
                return File(outStream.ToArray(), "application/pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }




        [Route("PrintEMultiBills")]
        [HttpPost]
        public async Task<IActionResult> PrintEMultiBills([FromBody] PrintBillRequest request)
        {
            try
            {

                // Optional: Validate other fields
                if (
                    string.IsNullOrEmpty(request.category) ||
                    string.IsNullOrEmpty(request.block) ||
                    string.IsNullOrEmpty(request.month) ||
                    string.IsNullOrEmpty(request.year))
                {
                    return BadRequest("All fields must be provided.");
                }

                // Optional: Log or process request info
                Console.WriteLine($"Generating bills for Project: {request.project}, Sector: {request.sector}, Block: {request.block}, Month: {request.month}, Year: {request.year}");

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));

                // Working URLs
                var url = $"http://172.20.228.2:81/api/ElectricityBill/GetEBill?category={request.category}&block={request.block}&month={request.month}&year={request.year}&project={request.project}";


                // RDLC URLs
                //var url = $"http://172.20.228.2:88/api/ElectricityBill/GetEBill?category={request.category}&block={request.block}&month={request.month}&year={request.year}&project={request.project}";


                // If needed, you can append filters to the URL or send them in headers/body to the API.
                // For now, we just log them.

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var pdfData = await response.Content.ReadAsByteArrayAsync();

                    if (pdfData == null || pdfData.Length == 0)
                    {
                        return BadRequest("Received empty PDF data");
                    }

                    Response.Headers.Add("Content-Disposition", "attachment; filename=MaintenanceBill.pdf");
                    return File(pdfData, "application/pdf");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, $"API Error: {errorContent}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }












        [Route("PrintEMultiBillsAMI")]
        [HttpPost]
        public async Task<IActionResult> PrintEMultiBillsAMI([FromBody] PrintBillRequest request)
        {
            try
            {
                if (
                    string.IsNullOrEmpty(request.category) ||
                    string.IsNullOrEmpty(request.block) ||
                    string.IsNullOrEmpty(request.month) ||
                    string.IsNullOrEmpty(request.year) ||
                    string.IsNullOrEmpty(request.tariffType))
                {
                    return BadRequest("All fields must be provided.");
                }

                Console.WriteLine($"Generating AMI bills for Project: {request.project}, Block: {request.block}, Category: {request.category}, Month: {request.month}, Year: {request.year}, TariffType: {request.tariffType}");

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));

                // AMI bill API — parameters dynamically built from dropdown selections
                var url = $"http://172.20.228.2:81/api/ElectricityBillsAMI/GetAMIBill" +
                          $"?block={Uri.EscapeDataString(request.block)}" +
                          $"&Category={Uri.EscapeDataString(request.category)}" +
                          $"&month={Uri.EscapeDataString(request.month)}" +
                          $"&year={Uri.EscapeDataString(request.year)}" +
                          $"&Project={Uri.EscapeDataString(request.project ?? "")}" +
                          $"&TariffType={Uri.EscapeDataString(request.tariffType)}";

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var pdfData = await response.Content.ReadAsByteArrayAsync();

                    if (pdfData == null || pdfData.Length == 0)
                    {
                        return BadRequest("Received empty PDF data");
                    }

                    Response.Headers.Add("Content-Disposition", "attachment; filename=ElectricityBillAMI.pdf");
                    return File(pdfData, "application/pdf");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, $"API Error: {errorContent}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }




        [Route("PrintEMultiBillsNM")]
        [HttpPost]
        public async Task<IActionResult> PrintEMultiBillsNM([FromBody] PrintBillRequest request)
        {
            try
            {

                // Optional: Validate other fields
                if (
                    string.IsNullOrEmpty(request.category) ||
                    string.IsNullOrEmpty(request.block) ||
                    string.IsNullOrEmpty(request.month) ||
                    string.IsNullOrEmpty(request.year))
                {
                    return BadRequest("All fields must be provided.");
                }

                // Optional: Log or process request info
                Console.WriteLine($"Generating bills for Project: {request.project}, Sector: {request.sector}, Block: {request.block}, Month: {request.month}, Year: {request.year}");

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));

                // Working URLs
                var url = $"http://172.20.228.2:81/api/ElectricityBillsNetMeter/GetNetMeterBill?category={request.category}&block={request.block}&month={request.month}&year={request.year}&project={request.project}";

                // RDLC URLs
                //var url = $"http://172.20.228.2:88/api/NetMeterBill/GetNMBill?category={request.category}&block={request.block}&month={request.month}&year={request.year}&project={request.project}";



                // If needed, you can append filters to the URL or send them in headers/body to the API.
                // For now, we just log them.

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var pdfData = await response.Content.ReadAsByteArrayAsync();

                    if (pdfData == null || pdfData.Length == 0)
                    {
                        return BadRequest("Received empty PDF data");
                    }

                    Response.Headers.Add("Content-Disposition", "attachment; filename=MaintenanceBill.pdf");
                    return File(pdfData, "application/pdf");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, $"API Error: {errorContent}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }













        [HttpPost]
        public IActionResult PayEMultiBill([FromBody] List<string> uids)
        {
            if (uids == null || uids.Count == 0)
            {
                return BadRequest("No bills selected.");
            }

            try
            {
                int processedCount = 0;
                int alreadyPaidCount = 0;
                List<string> alreadyPaidBills = new List<string>();

                foreach (var uid in uids)
                {
                    var bill = _dbContext.ElectricityBills.FirstOrDefault(b => b.Uid.ToString() == uid);
                    if (bill != null)
                    {
                        if (bill.PaymentStatus == "Paid")
                        {
                            alreadyPaidBills.Add(uid);
                            alreadyPaidCount++; // Increment already paid count
                            continue; // Skip already paid bills
                        }

                        bill.PaymentStatus = "Paid";
                        bill.PaymentDate = DateOnly.FromDateTime(DateTime.Now);
                        processedCount++;
                    }
                }

                _dbContext.SaveChanges();

                return Ok(new
                {
                    message = $"Successfully Paid {processedCount} bills! already Paid Bills Are {alreadyPaidCount} ",
                    processedCount = processedCount,
                    alreadyPaidCount = alreadyPaidCount,
                    processedUids = uids.Except(alreadyPaidBills),
                    alreadyPaidUids = alreadyPaidBills
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while processing payments.");
            }
        }






        public IActionResult SearchBills(string search)
        {
            // If search is empty, return an empty list
            if (string.IsNullOrEmpty(search))
            {
                return View(new List<ElectricityBill>());
            }

            //var bills = _dbContext.ElectricityBills
            //.Select(b => new ElectricityBill
            //{
            //    Uid = b.Uid,
            //    InvoiceNo = b.InvoiceNo,
            //    CustomerNo = b.CustomerNo,
            //    CustomerName = b.CustomerName,
            //    Btno = b.Btno,
            //    BillingMonth = b.BillingMonth,
            //    BillingYear = b.BillingYear,
            //    Opc = b.Opc ?? 0m // Ensure Opc is decimal
            //})
            //.Where(b => (b.Btno != null && b.Btno.Contains(search)) ||
            //            (b.CustomerName != null && b.CustomerName.Contains(search)))
            //.ToList();

            var query = _dbContext.ElectricityBills
                .Where(b => (b.Btno != null && b.Btno.Contains(search)) ||
                            (b.CustomerName != null && b.CustomerName.Contains(search)))
                .ToList();


            return View(query);
        }











        public IActionResult BillReport()
        {
            var model = new BillReportViewModel
            {
                Months = Enumerable.Range(1, 12).Select(i => new SelectListItem
                {
                    Value = new DateTime(2000, i, 1).ToString("MMMM"),
                    Text = new DateTime(2000, i, 1).ToString("MMMM")
                }).ToList(),

                Years = _dbContext.ElectricityBills
                    .Select(b => b.BillingYear)
                    .Distinct()
                    .OrderByDescending(y => y)
                    .Select(y => new SelectListItem { Value = y, Text = y })
                    .ToList(),

                TotalCustomers = _dbContext.ElectricityBills.Select(b => b.Btno).Distinct().Count()
            };

            return View(model);
        }



        [HttpPost]
        public async Task<IActionResult> BillReport(BillReportViewModel model)
        {
            if (!string.IsNullOrEmpty(model.SelectedMonth) && !string.IsNullOrEmpty(model.SelectedYear))
            {
                // Get total customers from CustomersDetail table
                var totalCustomers = await _dbContext.CustomersDetails
                    .Select(c => c.Btno)
                    .Distinct()
                    .CountAsync();

                // Get total bills created for selected month & year
                var billsCreated = await _dbContext.ElectricityBills
                    .Where(b => b.BillingMonth == model.SelectedMonth && b.BillingYear == model.SelectedYear)
                    .Select(b => b.Btno)
                    .Distinct()
                    .CountAsync();

                // Calculate pending bills
                var pendingBills = totalCustomers - billsCreated;

                // Assign values to model
                model.TotalCustomers = totalCustomers;
                model.TotalBillsCreated = billsCreated;
                model.PendingBills = pendingBills;
            }

            // Keep dropdowns populated
            model.Months = Enumerable.Range(1, 12).Select(i => new SelectListItem
            {
                Value = new DateTime(2000, i, 1).ToString("MMMM"),
                Text = new DateTime(2000, i, 1).ToString("MMMM")
            }).ToList();

            model.Years = _dbContext.ElectricityBills
                .Select(b => b.BillingYear)
                .Distinct()
                .OrderByDescending(y => y)
                .Select(y => new SelectListItem { Value = y, Text = y })
                .ToList();

            return View(model);
        }






        public async Task<IActionResult> GeneratedBillsReport(string SelectedMonth, string SelectedYear)
        {
            var model = new ModedGenertedBillReport
            {
                SelectedMonth = SelectedMonth,
                SelectedYear = SelectedYear
            };

            var customers = await _dbContext.CustomersDetails.ToListAsync();
            var bills = await _dbContext.ElectricityBills
                .Where(b => b.BillingMonth == SelectedMonth && b.BillingYear == SelectedYear)
                .ToListAsync();

            model.GeneratedBills = customers.Select(c => new GeneratedBill
            {
                CustomerName = c.CustomerName,
                CustomerID = c.Btno,
                BillingMonth = SelectedMonth,
                BillingYear = SelectedYear,
                BillAmount = bills.FirstOrDefault(b => b.Btno == c.Btno)?.BillAmount ?? 0,
                BillStatus = bills.Any(b => b.Btno == c.Btno) ? "Created" : "Pending"
            }).ToList();

            return View(model);
        }







        private const int SearchBillPageSize = 50;

        [HttpGet]
        public IActionResult SearchBill(string? search, string? month, string? year, int? page)
        {
            var pagedBills = BuildSearchBillByKeyword(search, month, year, page);
            return View(pagedBills);
        }

        [HttpGet]
        public IActionResult PrintBill(string? month, string? year, string? BtNo, string? block, int? page)
        {
            var pagedBills = BuildBillSearchResults(month, year, BtNo, block, page);
            return View(pagedBills);
        }

        [HttpPost]
        public IActionResult SearchBillPost(string? search, string? month, string? year)
        {
            return RedirectToAction(nameof(SearchBill), new { search, month, year, page = 1 });
        }

        /// <summary>
        /// Prints selected bills via ElectricitySingleBill API using parallel BTNo / Month / Year arrays.
        /// Example: .../GetEBill?BillingMonths=July,July&BillingYears=2026,2026&BTNo=BTL-10014,BTL-10000
        /// </summary>
        [Route("PrintElectricitySingleBills")]
        [HttpPost]
        public async Task<IActionResult> PrintElectricitySingleBills([FromBody] ElectricitySingleBillPrintRequest request)
        {
            try
            {
                if (request?.Items == null || request.Items.Count == 0)
                {
                    return BadRequest("Please select at least one bill to print.");
                }

                var btNos = new List<string>();
                var months = new List<string>();
                var years = new List<string>();

                foreach (var item in request.Items)
                {
                    var btNo = item.BtNo?.Trim();
                    var month = item.Month?.Trim();
                    var year = item.Year?.Trim();

                    if (string.IsNullOrWhiteSpace(btNo)
                        || string.IsNullOrWhiteSpace(month)
                        || string.IsNullOrWhiteSpace(year))
                    {
                        return BadRequest("Each selected bill must include BTNo, Month and Year.");
                    }

                    btNos.Add(btNo);
                    months.Add(month);
                    years.Add(year);
                }

                var url =
                    "http://172.20.228.2:81/api/ElectricitySingleBill/GetEBill" +
                    $"?BillingMonths={Uri.EscapeDataString(string.Join(",", months))}" +
                    $"&BillingYears={Uri.EscapeDataString(string.Join(",", years))}" +
                    $"&BTNo={Uri.EscapeDataString(string.Join(",", btNos))}";

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(120);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));

                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, $"API Error: {errorContent}");
                }

                var pdfData = await response.Content.ReadAsByteArrayAsync();
                if (pdfData == null || pdfData.Length == 0)
                {
                    return BadRequest("Received empty PDF data from print service.");
                }

                Response.Headers["Content-Disposition"] = "attachment; filename=ElectricitySingleBills.pdf";
                return File(pdfData, "application/pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Search bills by keyword (BTNo / Name / CNIC). Optional BillingMonth and BillingYear narrow results.
        /// Search text is required.
        /// </summary>
        private IPagedList<BillDTO> BuildSearchBillByKeyword(string? search, string? month, string? year, int? page)
        {
            search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
            month = string.IsNullOrWhiteSpace(month) ? null : month.Trim();
            year = string.IsNullOrWhiteSpace(year) ? null : year.Trim();

            ViewBag.SelectedSearch = search;
            ViewBag.SelectedMonth = month;
            ViewBag.SelectedYear = year;

            if (string.IsNullOrEmpty(search))
            {
                if (!string.IsNullOrEmpty(month) || !string.IsNullOrEmpty(year))
                {
                    ViewBag.ErrorMessage = "Please enter BTNo, Name or CNIC to search. Month and Year are optional filters only.";
                }
                return new StaticPagedList<BillDTO>(Array.Empty<BillDTO>(), 1, SearchBillPageSize, 0);
            }

            var term = search.ToLower();

            var baseQuery =
                from bill in _dbContext.ElectricityBills
                join customer in _dbContext.CustomersDetails
                    on bill.Btno equals customer.Btno
                where (bill.Btno != null && bill.Btno.ToLower().Contains(term))
                      || (customer.CustomerName != null && customer.CustomerName.ToLower().Contains(term))
                      || (customer.Cnicno != null && customer.Cnicno.ToLower().Contains(term))
                select new { bill, customer };

            if (!string.IsNullOrEmpty(month))
            {
                baseQuery = baseQuery.Where(x => x.bill.BillingMonth == month);
            }

            if (!string.IsNullOrEmpty(year))
            {
                baseQuery = baseQuery.Where(x => x.bill.BillingYear == year);
            }

            var ordered = baseQuery
                .OrderBy(x => x.customer.Block)
                .ThenBy(x => x.bill.Btno)
                .ThenByDescending(x => x.bill.BillingYear)
                .ThenBy(x => x.bill.BillingMonth)
                .Select(x => new BillDTO
                {
                    Uid = x.bill.Uid,
                    CustomerNo = x.customer.CustomerNo,
                    Btno = x.bill.Btno,
                    CustomerName = x.customer.CustomerName,
                    Cnicno = x.customer.Cnicno,
                    FatherName = x.customer.FatherName,
                    InstalledOn = x.customer.InstalledOn,
                    MobileNo = x.customer.MobileNo,
                    TelephoneNo = x.customer.TelephoneNo,
                    Ntnnumber = x.customer.Ntnnumber,
                    City = x.customer.City,
                    Project = x.customer.Project,
                    SubProject = x.customer.SubProject,
                    TariffName = x.customer.TariffName,
                    BankNo = x.customer.BankNo,
                    BtnoMaintenance = x.customer.BtnoMaintenance,
                    Category = x.customer.Category,
                    Block = x.customer.Block,
                    PlotType = x.customer.PlotType,
                    Size = x.customer.Size,
                    Sector = x.customer.Sector,
                    PloNo = x.customer.PloNo,
                    BillStatusMaint = x.customer.BillStatusMaint,
                    BillStatus = x.customer.BillStatus,
                    InvoiceNo = x.bill.InvoiceNo,
                    BillingMonth = x.bill.BillingMonth,
                    BillingYear = x.bill.BillingYear,
                    BillingDate = x.bill.BillingDate,
                    DueDate = x.bill.DueDate,
                    IssueDate = x.bill.IssueDate,
                    ValidDate = x.bill.ValidDate,
                    PaymentStatus = x.bill.PaymentStatus,
                    PaymentDate = x.bill.PaymentDate,
                    PaymentMethod = x.bill.PaymentMethod,
                    BankDetail = x.bill.BankDetail,
                    BillAmountInDueDate = x.bill.BillAmountInDueDate,
                    BillSurcharge = x.bill.BillSurcharge,
                    BillAmountAfterDueDate = x.bill.BillAmountAfterDueDate
                });

            int pageNumber = page ?? 1;
            var pagedBills = ordered.ToPagedList(pageNumber, SearchBillPageSize);

            if (pagedBills.TotalItemCount == 0)
            {
                ViewBag.ErrorMessage = "No bills found for the provided search.";
            }

            return pagedBills;
        }

        private IPagedList<BillDTO> BuildBillSearchResults(
            string? month,
            string? year,
            string? BtNo,
            string? block,
            int? page)
        {
            month = string.IsNullOrWhiteSpace(month) ? null : month.Trim();
            year = string.IsNullOrWhiteSpace(year) ? null : year.Trim();
            BtNo = string.IsNullOrWhiteSpace(BtNo) ? null : BtNo.Trim();
            block = string.IsNullOrWhiteSpace(block) ? null : block.Trim();

            ViewBag.SelectedMonth = month;
            ViewBag.SelectedYear = year;
            ViewBag.SelectedBtNo = BtNo;
            ViewBag.SelectedBlock = block;
            PopulateSearchBillBlocks(block);

            var hasBtNo = !string.IsNullOrEmpty(BtNo);
            var hasMonth = !string.IsNullOrEmpty(month);
            var hasYear = !string.IsNullOrEmpty(year);
            var hasBlock = !string.IsNullOrEmpty(block);
            var hasAnyFilter = hasBtNo || hasMonth || hasYear || hasBlock;

            if (!hasAnyFilter)
            {
                return new StaticPagedList<BillDTO>(Array.Empty<BillDTO>(), 1, SearchBillPageSize, 0);
            }

            if (hasBlock && (!hasMonth || !hasYear) && !hasBtNo)
            {
                ViewBag.ErrorMessage = "When filtering by Block, please also select Month and Year.";
                return new StaticPagedList<BillDTO>(Array.Empty<BillDTO>(), 1, SearchBillPageSize, 0);
            }

            if ((hasMonth && !hasYear) || (!hasMonth && hasYear))
            {
                ViewBag.ErrorMessage = "Please select both Month and Year together, or search by BTNo only.";
                return new StaticPagedList<BillDTO>(Array.Empty<BillDTO>(), 1, SearchBillPageSize, 0);
            }

            var baseQuery =
                from bill in _dbContext.ElectricityBills
                join customer in _dbContext.CustomersDetails
                    on bill.Btno equals customer.Btno
                select new { bill, customer };

            if (hasBtNo)
            {
                baseQuery = baseQuery.Where(x => x.bill.Btno != null && x.bill.Btno.Trim() == BtNo);
            }

            if (hasMonth && hasYear)
            {
                baseQuery = baseQuery.Where(x => x.bill.BillingMonth == month && x.bill.BillingYear == year);
            }

            if (hasBlock)
            {
                baseQuery = baseQuery.Where(x => x.customer.Block != null && x.customer.Block.Trim() == block);
            }

            var ordered = baseQuery
                .OrderBy(x => x.customer.Block)
                .ThenBy(x => x.bill.Btno)
                .ThenByDescending(x => x.bill.BillingYear)
                .ThenBy(x => x.bill.BillingMonth)
                .Select(x => new BillDTO
                {
                    Uid = x.bill.Uid,
                    CustomerNo = x.customer.CustomerNo,
                    Btno = x.bill.Btno,
                    CustomerName = x.customer.CustomerName,
                    Cnicno = x.customer.Cnicno,
                    FatherName = x.customer.FatherName,
                    InstalledOn = x.customer.InstalledOn,
                    MobileNo = x.customer.MobileNo,
                    TelephoneNo = x.customer.TelephoneNo,
                    Ntnnumber = x.customer.Ntnnumber,
                    City = x.customer.City,
                    Project = x.customer.Project,
                    SubProject = x.customer.SubProject,
                    TariffName = x.customer.TariffName,
                    BankNo = x.customer.BankNo,
                    BtnoMaintenance = x.customer.BtnoMaintenance,
                    Category = x.customer.Category,
                    Block = x.customer.Block,
                    PlotType = x.customer.PlotType,
                    Size = x.customer.Size,
                    Sector = x.customer.Sector,
                    PloNo = x.customer.PloNo,
                    BillStatusMaint = x.customer.BillStatusMaint,
                    BillStatus = x.customer.BillStatus,
                    InvoiceNo = x.bill.InvoiceNo,
                    BillingMonth = x.bill.BillingMonth,
                    BillingYear = x.bill.BillingYear,
                    BillingDate = x.bill.BillingDate,
                    DueDate = x.bill.DueDate,
                    IssueDate = x.bill.IssueDate,
                    ValidDate = x.bill.ValidDate,
                    PaymentStatus = x.bill.PaymentStatus,
                    PaymentDate = x.bill.PaymentDate,
                    PaymentMethod = x.bill.PaymentMethod,
                    BankDetail = x.bill.BankDetail,
                    BillAmountInDueDate = x.bill.BillAmountInDueDate,
                    BillSurcharge = x.bill.BillSurcharge,
                    BillAmountAfterDueDate = x.bill.BillAmountAfterDueDate
                });

            int pageNumber = page ?? 1;
            var pagedBills = ordered.ToPagedList(pageNumber, SearchBillPageSize);

            if (pagedBills.TotalItemCount == 0)
            {
                ViewBag.ErrorMessage = "No bills found for the provided criteria.";
            }

            return pagedBills;
        }

        private void PopulateSearchBillBlocks(string? selectedBlock)
        {
            ViewBag.Blocks = _dbContext.CustomersDetails
                .Where(c => c.Block != null && c.Block.Trim() != "")
                .Select(c => c.Block!.Trim())
                .Distinct()
                .OrderBy(b => b)
                .ToList();
            ViewBag.SelectedBlock = selectedBlock;
        }


        private string NaturalSortKey(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return Regex.Replace(input, @"\d+", match => match.Value.PadLeft(10, '0'));
        }


        //[HttpPost]
        //public async Task<IActionResult> GeneratedBillsReport(BillReportViewModel model)
        //{
        //    if (!string.IsNullOrEmpty(model.SelectedMonth) && !string.IsNullOrEmpty(model.SelectedYear))
        //    {
        //        var customers = await _dbContext.CustomersDetails.ToListAsync(); // Fetch all customers

        //        var billedCustomers = await _dbContext.ElectricityBills
        //            .Where(b => b.BillingMonth == model.SelectedMonth && b.BillingYear == model.SelectedYear)
        //            .Select(b => b.Btno)
        //            .ToListAsync();

        //        model.TotalCustomers = customers.Count;
        //        model.TotalBillsCreated = billedCustomers.Count;
        //        model.PendingBills = model.TotalCustomers - model.TotalBillsCreated;

        //        // Get detailed customer records (both billed and pending)
        //        model.BilledCustomers = customers.Where(c => billedCustomers.Contains(c.Btno)).ToList();
        //        model.PendingCustomers = customers.Where(c => !billedCustomers.Contains(c.Btno)).ToList();
        //    }

        //    // Maintain month & year lists
        //    model.Months = Enumerable.Range(1, 12).Select(i => new SelectListItem
        //    {
        //        Value = new DateTime(2000, i, 1).ToString("MMMM"),
        //        Text = new DateTime(2000, i, 1).ToString("MMMM")
        //    }).ToList();

        //    model.Years = _dbContext.ElectricityBills
        //        .Select(b => b.BillingYear)
        //        .Distinct()
        //        .OrderByDescending(y => y)
        //        .Select(y => new SelectListItem { Value = y, Text = y })
        //        .ToList();

        //    return View(model);
        //}






        public IActionResult Customers()
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var projects = _dbContext.Configurations
                .AsNoTracking()
                .Where(c => c.ConfigKey != null &&
                            c.ConfigValue != null &&
                            c.ConfigKey.Trim().ToLower() == "project" &&
                            c.ConfigValue.Trim() != "")
                .Select(c => c.ConfigValue!.Trim())
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            var model = new EBillCustomerFilterViewModel
            {
                Projects = projects,
                Blocks = new List<string>(),
                Customers = new List<CustomersDetail>().ToPagedList(1, 20)
            };

            return View(model);
        }

        [HttpGet]
        public JsonResult GetCustomerBlocksByProject(string project)
        {
            var blocksQuery = _dbContext.CustomersDetails.AsQueryable();

            if (!string.IsNullOrWhiteSpace(project))
            {
                blocksQuery = blocksQuery.Where(c => c.Project == project);
            }

            var blocks = blocksQuery
                .Select(c => c.Block)
                .Where(b => b != null && b != "")
                .Distinct()
                .OrderBy(b => b)
                .ToList();

            return Json(blocks);
        }

        [HttpGet]
        public PartialViewResult FilterCustomers(string project, string block, string btNo, int? page)
        {
            var query = _dbContext.CustomersDetails.AsQueryable();

            if (!string.IsNullOrWhiteSpace(project))
            {
                query = query.Where(c => c.Project == project);
            }

            if (!string.IsNullOrWhiteSpace(block))
            {
                query = query.Where(c => c.Block == block);
            }

            if (!string.IsNullOrWhiteSpace(btNo))
            {
                var term = btNo.Trim();
                query = query.Where(c =>
                    (c.Btno != null && c.Btno.Contains(term)) ||
                    (c.PloNo != null && c.PloNo.Contains(term)));
            }

            const int pageSize = 20;
            var pageNumber = page ?? 1;

            var customers = query
                .OrderBy(c => c.Project)
                .ThenBy(c => c.Block)
                .ThenBy(c => c.Btno)
                .ToPagedList(pageNumber, pageSize);

            return PartialView("_EBillCustomersGrid", customers);
        }

    }

}

