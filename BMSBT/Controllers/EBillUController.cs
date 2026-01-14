using BMSBT.BillServices;
using BMSBT.DTO;
using BMSBT.Models;
using BMSBT.Requests;
using BMSBT.Roles;
using BMSBT.ViewModels;
using DevExpress.CodeParser;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using X.PagedList.Extensions;
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





        // Reading Dashboard - reuse billing dashboard functionality
        public IActionResult Index(string month, string year, string project)
        {
            var model = new DTO.DashboardViewModel();

            // Defaults to current month/year if none provided
            string currentMonth = DateTime.Now.ToString("MMMM");
            string currentYear = DateTime.Now.Year.ToString();

            month = string.IsNullOrEmpty(month) ? currentMonth : month;
            year = string.IsNullOrEmpty(year) ? currentYear : year;

            try
            {
                model.Projects = new List<string>();
                model.Years = new List<string>();
                model.Months = new List<string>();

                var billingReportQuery = @"EXEC sp_GetBillingReportFormatted @Month = {0}, @Year = {1}, @Project = {2}";

                var billingReportData = _dbContext.BillingReportData
                    .FromSqlRaw(billingReportQuery,
                        string.IsNullOrEmpty(month) ? (object)DBNull.Value : month,
                        string.IsNullOrEmpty(year) ? (object)DBNull.Value : year,
                        string.IsNullOrEmpty(project) ? (object)DBNull.Value : project)
                    .ToList();

                if (billingReportData != null && billingReportData.Any())
                {
                    var generatedDetail = billingReportData.Where(x => x.Section == "Generated Detail").ToList();
                    var netMeteringDetail = billingReportData.Where(x => x.Section == "Generated Detail - Net Metering").ToList();
                    var paymentsDetail = billingReportData.Where(x => x.Section == "Payments Detail").ToList();

                    var billsGenerated = generatedDetail.FirstOrDefault(x => x.Metric == "Bills Generated");
                    var totalCurrentBilling = generatedDetail.FirstOrDefault(x => x.Metric == "Total Current Billing (Rs)");

                    if (billsGenerated != null)
                    {
                        model.TotalBillsGenerated = Convert.ToInt32(billsGenerated.Value);
                        model.BillsUnits = billsGenerated.SecondaryValue ?? 0;
                    }

                    if (totalCurrentBilling != null)
                    {
                        model.TotalBillAmountGenerated = totalCurrentBilling.Value;
                        if (model.BillsUnits == 0)
                        {
                            model.BillsUnits = totalCurrentBilling.SecondaryValue ?? 0;
                        }
                    }

                    var netMeterBillsGenerated = netMeteringDetail.FirstOrDefault(x => x.Metric == "Bills Generated");
                    var netMeterTotalBilling = netMeteringDetail.FirstOrDefault(x => x.Metric == "Total Current Billing (Rs)");

                    if (netMeterBillsGenerated != null)
                    {
                        model.NetMeterBillsGenerated = Convert.ToInt32(netMeterBillsGenerated.Value);
                        model.NetMeterBillsUnits = netMeterBillsGenerated.SecondaryValue ?? 0;
                    }

                    if (netMeterTotalBilling != null)
                    {
                        model.NetMeterTotalBilling = netMeterTotalBilling.Value;
                    }

                    var billPaidAmount = paymentsDetail.FirstOrDefault(x => x.Metric == "Bill Paid (Amount)");
                    var paidBillsCount = paymentsDetail.FirstOrDefault(x => x.Metric == "Paid (No Of Bills)");

                    if (billPaidAmount != null)
                    {
                        model.TotalBillAmountCollected = billPaidAmount.Value;
                        model.BillUnpaidAmount = Convert.ToDecimal(billPaidAmount.SecondaryValue ?? 0);
                    }

                    if (paidBillsCount != null)
                    {
                        model.TotalBillsPaid = Convert.ToInt32(paidBillsCount.Value);
                        model.UnpaidBillsCount = paidBillsCount.SecondaryValue ?? 0;
                    }
                }

                model.BillingReportData = billingReportData;

                ViewBag.SelectedMonth = month;
                ViewBag.SelectedYear = year;
                ViewBag.SelectedProject = project;
                ViewBag.BillingPeriod = $"{year} - {month}";

                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"An error occurred while loading the dashboard data: {ex.Message}");
                return View(new DTO.DashboardViewModel());
            }
        }






        public IActionResult GenerateBillMain(string project, string sector, string block, int? page)
        {
            if (HttpContext.Session.GetInt32("UserName") == null)
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
            if (HttpContext.Session.GetInt32("UserName") == null)
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
            ViewBag.Username = HttpContext.Session.GetString("UserName");

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
            var results = new List<string>();

            var resultss = new List<string>();

            // Generate bills for each selected customer ID
            foreach (var customerId in request.SelectedIds)
            {
                // Call the function to generate the bill for each customer
                var result = ElectrcityFunctions.GenerateEBillForCustomer(customerId, billingMonth, billingYear, previousMonth, previousYear, IssueDate, DueDate,ValidDate, ViewBag.UserName , FPAMONTH1,FPAYEAR1,FPARATE1, FPAMONTH2, FPAYEAR2, FPARATE2);
                resultss.Add(result);
            }

            return new JsonResult(new { success = true, message = "Bills generated successfully" });
        }











        [Route("PrintEMultiBill")]
        [HttpPost]
        public async Task<IActionResult> PrintEMultiBill([FromBody] PrintBillRequest request)
        {
            try
            {

                // Optional: Validate other fields
                //if (string.IsNullOrEmpty(request.project) ||
                //    string.IsNullOrEmpty(request.sector) ||
                //    string.IsNullOrEmpty(request.block) ||
                //    string.IsNullOrEmpty(request.month) ||
                //    string.IsNullOrEmpty(request.year))
                //{
                //    return BadRequest("All fields must be provided.");
                //}

                // Optional: Log or process request info
                Console.WriteLine($"Generating bills for Project: {request.project}, Sector: {request.sector}, Block: {request.block}, Month: {request.month}, Year: {request.year}");

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));

                var url = $"http://172.20.229.3:84/api/ElectricityBill/GetEBillByUid?uids={request.uids}";

                //var url = $"http://172.20.229.3:84/api/ElectricityBill/GetEBillByUid?project={request.project}&sector={request.sector}&block={request.block}&month={request.month}&year={request.year}";


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
                //var url = $"http://172.20.228.2/api/ElectricityBill/GetEBill?category={request.category}&block={request.block}&month={request.month}&year={request.year}&project={request.project}";





                // var url = $"http://172.20.229.3:84/api/ElectricityBill/GetEBillByUid?uids={request.uids}";

                //var url = $"http://172.20.229.3:84/api/ElectricityBill/GetEBill?category={request.category}&block={request.block}&month={request.month}&year={request.year}&project={request.project}";
                //var url = $"http://172.20.228.2/api/EBill01/GetEBill?category={request.category}&block={request.block}&month={request.month}&year={request.year}&project={request.project}";

                //var url = $"http://172.20.228.2/api/EBill/PrintEBills";

                //var url = $"http://172.20.228.2/api/EBill01/GetEBill" +
                //$"?block={Uri.EscapeDataString(request.block)}" +
                //$"&Category={Uri.EscapeDataString(request.category)}" +
                //$"&month={Uri.EscapeDataString(request.month)}" +
                //$"&year={Uri.EscapeDataString(request.year)}" +
                //$"&Project={Uri.EscapeDataString(request.project)}";





                //url = $"http://172.20.228.2/api/ElectricityBill/GetEBill?block=Safari%20Villas&Category=Commercial&month=October&year=2025&Project=Mohlanwal";

                //url = $"http://172.20.228.2/api/ElectricityBill/GetEBill?block=Safari%20Villas&Category=Residential&month=October&year=2025&Project=Mohlanwal";


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
                //var url = $"http://172.20.228.2/api/ElectricityBill/GetEBill?category={request.category}&block={request.block}&month={request.month}&year={request.year}&project={request.project}";





                // var url = $"http://172.20.229.3:84/api/ElectricityBill/GetEBillByUid?uids={request.uids}";

                //var url = $"http://172.20.229.3:84/api/ElectricityBill/GetEBill?category={request.category}&block={request.block}&month={request.month}&year={request.year}&project={request.project}";
                //var url = $"http://172.20.228.2/api/EBill01/GetEBill?category={request.category}&block={request.block}&month={request.month}&year={request.year}&project={request.project}";

                //var url = $"http://172.20.228.2/api/EBill/PrintEBills";

                //var url = $"http://172.20.228.2/api/EBill01/GetEBill" +
                //$"?block={Uri.EscapeDataString(request.block)}" +
                //$"&Category={Uri.EscapeDataString(request.category)}" +
                //$"&month={Uri.EscapeDataString(request.month)}" +
                //$"&year={Uri.EscapeDataString(request.year)}" +
                //$"&Project={Uri.EscapeDataString(request.project)}";





                //url = $"http://172.20.228.2/api/ElectricityBill/GetEBill?block=Safari%20Villas&Category=Commercial&month=October&year=2025&Project=Mohlanwal";

                //url = $"http://172.20.228.2/api/ElectricityBill/GetEBill?block=Safari%20Villas&Category=Residential&month=October&year=2025&Project=Mohlanwal";


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







        [HttpGet]
        public async Task<IActionResult> SearchBill()
        {
            return View();
        }



        [HttpPost]
        public IActionResult SearchBill(string? month, string? year, string? BtNo)
        {
            // If nothing is provided
            if (string.IsNullOrEmpty(BtNo) && string.IsNullOrEmpty(month) && string.IsNullOrEmpty(year))
            {
                ViewBag.ErrorMessage = "Please select a month/year or enter a Bill No.";
                return View("SearchBill");
            }

            var query = from bill in _dbContext.ElectricityBills
                        join customer in _dbContext.CustomersDetails
                            on bill.Btno equals customer.Btno
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
                        };

            // Apply filters based on inputs
            if (!string.IsNullOrEmpty(BtNo))
            {
                query = query.Where(b => b.Btno == BtNo);

                if (!string.IsNullOrEmpty(month) && !string.IsNullOrEmpty(year))
                {
                    query = query.Where(b => b.BillingMonth == month && b.BillingYear == year);
                }
            }
            else if (!string.IsNullOrEmpty(month) && !string.IsNullOrEmpty(year))
            {
                // BtNo is empty, filter by month/year only
                query = query.Where(b => b.BillingMonth == month && b.BillingYear == year);
            }

            var bills = query.ToList();

            if (!bills.Any())
            {
                ViewBag.ErrorMessage = "No bills found for the provided criteria.";
            }

            var pagedBills = bills.ToPagedList(1, 5000);
            return View("SearchBill", pagedBills);
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







    }

}

