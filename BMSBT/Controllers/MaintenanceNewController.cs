using BMSBT.BillServices;
using BMSBT.Models;
using BMSBT.Requests;
using BMSBT.ViewModels;
using BMSBT.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data.Entity;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using X.PagedList;
using X.PagedList.Extensions;
using X.PagedList.Mvc.Core;
using static BMSBT.Controllers.MaintenanceBillController;

namespace BMSBT.Controllers
{
    public class MaintenanceNewController : Controller
    {
        private readonly BmsbtContext _dbContext;
        private readonly MaintenanceFunctions MaintenanceFunctions;
        private readonly ICurrentOperatorService _operatorService;
        private readonly IHttpClientFactory _httpClientFactory;

       
        public MaintenanceNewController(IHttpClientFactory httpClientFactory, BmsbtContext context, ICurrentOperatorService operatorService)
        {
            _dbContext = context;
            MaintenanceFunctions = new MaintenanceFunctions(_dbContext);
            _operatorService = operatorService;
            _httpClientFactory = httpClientFactory;
        
        }






        public IActionResult Index(string selectedYear, string selectedMonth)
        {

            ViewBag.Years = new List<SelectListItem>
    {
        new SelectListItem { Value = "2024", Text = "2024" },
        new SelectListItem { Value = "2025", Text = "2025" }
    };

            ViewBag.Months = new List<SelectListItem>
    {
                 new SelectListItem { Value = "Janurary", Text = "Janurary" },
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

            // Retain the selected values
            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedMonth = selectedMonth;


            // Check if both filters are provided
            if (string.IsNullOrEmpty(selectedYear) || string.IsNullOrEmpty(selectedMonth))
            {
                // Return empty data for the graph
                ViewBag.ChartLabels = new List<string>();
                ViewBag.ChartData = new List<int>();
                return View();
            }


            // ViewBag.Years = _context.ReadingSheets
            //.Select(r => r.Year)
            //.Distinct()
            //.OrderBy(y => y) // Ensure they are sorted
            //.ToList();

            //     ViewBag.Months = _context.ReadingSheets
            //         .Select(r => r.Month)
            //         .Distinct()
            //         .ToList();

            // Filter data based on selected year and month
            var filteredData = _dbContext.ReadingSheets.AsQueryable();

            if (!string.IsNullOrEmpty(selectedYear))
            {
                filteredData = filteredData.Where(r => r.Year == selectedYear);
            }

            if (!string.IsNullOrEmpty(selectedMonth))
            {
                filteredData = filteredData.Where(r => r.Month == selectedMonth && r.Year == selectedYear);
            }

            // Generate the data for the graph
            var totalMeters = filteredData.Count();

            var readingSheetData = filteredData
                .GroupBy(c => c.MeterType)
                .Select(group => new
                {
                    meters = group.Key,
                    Total = group.Count()
                })
                .ToList();

            var totalAllSubProjects = readingSheetData.Sum(x => x.Total);

            // Prepare data for the chart
            ViewBag.ChartLabels = readingSheetData.Select(x => x.meters).ToList();
            ViewBag.ChartLabels.Add("All SubProjects"); // Add a label for the total
            ViewBag.ChartData = readingSheetData.Select(x => x.Total).ToList();
            ViewBag.ChartData.Add(totalMeters); // Add the total as a separate data point

            //// Pass selected filters back to the view
            //ViewBag.SelectedYear = selectedYear;
            //ViewBag.SelectedMonth = selectedMonth;

            return View();
        }


        public IActionResult CustomersMaintenance()
        {
            var projects = _dbContext.CustomersMaintenance
                .Select(c => c.Project.Trim())
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            var customers = _dbContext.CustomersMaintenance
                .OrderBy(c => c.Project)
                .ThenBy(c => c.Block)
                .ThenBy(c => c.BTNo)
                .ToList();

            var model = new MaintenanceCustomerFilterViewModel
            {
                Projects = projects,
                Blocks = new List<string>(),
                Customers = customers
            };

            return View(model);
        }

        [HttpGet]
        public JsonResult GetBlocksByProject(string project)
        {
            var blocksQuery = _dbContext.CustomersMaintenance.AsQueryable();

            if (!string.IsNullOrWhiteSpace(project))
            {
                blocksQuery = blocksQuery.Where(c => c.Project == project);
            }

            var blocks = blocksQuery
                .Select(c => c.Block)
                .Distinct()
                .OrderBy(b => b)
                .ToList();

            return Json(blocks);
        }

        [HttpGet]
        public PartialViewResult FilterCustomers(string project, string block, string btNo)
        {
            var query = _dbContext.CustomersMaintenance.AsQueryable();

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
                query = query.Where(c => c.BTNo != null && c.BTNo.Contains(btNo));
            }

            var customers = query
                .OrderBy(c => c.Project)
                .ThenBy(c => c.Block)
                .ThenBy(c => c.BTNo)
                .ToList();

            return PartialView("_MaintenanceCustomersGrid", customers);
        }



        public IActionResult GenerateBill(string selectedProject, string btNoSearch)
        {
            // Dropdown projects
            var projects = _dbContext.CustomersMaintenance
                .Select(p => p.Project.Trim())
                .Distinct()
                .ToList();




            // Start with empty result
            var filteredData = new List<MaintSectorCustomersViewModel>();

            // Only load if project is selected
            if (!string.IsNullOrEmpty(selectedProject))
            {

                var query = _dbContext.CustomersMaintenance
            .Where(c =>
                (c.BillGenerationStatus == null || c.BillGenerationStatus == "Not Generated") &&
                c.Project.Trim() == selectedProject.Trim());

                if (!string.IsNullOrEmpty(btNoSearch))
                {
                    query = query.Where(c => c.BTNo.Contains(btNoSearch));
                }

                filteredData = query.GroupBy(c => c.Sector)
                .Select(g => new MaintSectorCustomersViewModel
                {
                    Sector = g.Key,
                    Customers = g.ToList()
                 }).ToList();

            }

            ViewBag.Projects = projects;
            ViewBag.SelectedProject = selectedProject;

            return View(filteredData);

        }




        [HttpPost]
        
        public async Task<IActionResult> GenerateMaintenanceBills([FromBody] MaintenanceBillRequest request)
        {
            // Set Operator Name
            string operatorId = HttpContext.Session.GetString("OperatorId");
            await _operatorService.InitializeAsync(operatorId);
            var currentOperator = _operatorService.GetCurrentOperator();

            // Check if CurrentMonth and CurrentYear are set
            if (string.IsNullOrEmpty(currentOperator.BillingMonth) || string.IsNullOrEmpty(currentOperator.BillingYear))
            {
                return Json(new { success = false, message = "Please Update Operator Setup" });
            }

            if (string.IsNullOrEmpty(operatorId))
            {
                return Json(new { success = false, message = "Operator ID not found in session" });
            }

            if (currentOperator == null)
            {
                return Json(new { success = false, message = "Operator details not found" });
            }



            string billingMonth = currentOperator.BillingMonth;
            string billingYear = currentOperator.BillingYear.ToString();

            if (string.IsNullOrEmpty(billingMonth) || string.IsNullOrEmpty(billingYear))
            {
                return Json(new { success = false, message = "Month and Year must be provided." });
            }


            MaintenanceFunctions.GetPreviousBillingPeriod(billingMonth, billingYear);
            string previousMonth = BillCreationState.PreviousMonth;
            string previousYear = BillCreationState.PreviousYear;
            DateOnly? IssueDate = currentOperator.IssueDate.HasValue
       ? DateOnly.FromDateTime(currentOperator.IssueDate.Value)
       : (DateOnly?)null;

            DateOnly? DueDate = currentOperator.DueDate.HasValue
                ? DateOnly.FromDateTime(currentOperator.DueDate.Value)
                : (DateOnly?)null;


            var results = new List<string>();

            // Generate bills for each selected customer ID
            foreach (var customerId in request.SelectedIds)
            {
                // Call the function to generate the bill for each customer
                var result = MaintenanceFunctions.GenerateBillForCustomer(customerId, billingMonth, billingYear, previousMonth, previousYear, IssueDate, DueDate);
                results.Add(result);
            }

            // Return a success message with the generated results
            return Json(new { success = true, message = "Results generated successfully!", results });
        }







        public IActionResult Generate(string project = null, string plotType = null, string plotSize = null)
        {
            var customers = _dbContext.CustomersDetails.AsQueryable();

            if (!string.IsNullOrEmpty(project))
            {
                customers = customers.Where(c => c.Project == project);
            }

            if (!string.IsNullOrEmpty(plotType))
            {
                customers = customers.Where(c => c.PlotType == plotType);
            }

            if (!string.IsNullOrEmpty(plotSize))
            {
                customers = customers.Where(c => c.Size == plotSize);
            }

            return View(customers.ToList());
        }





        //[HttpGet] // Changed from [HttpPost]
        //public IActionResult MaintenanceBillsSearch(string billingMonth, string billingYear, string block, string btNo, int? page)
        //{
        //    ViewBag.Months = GetMonths();
        //    ViewBag.Years = GetYears();

        //    var query = _dbContext.MaintenanceBills.AsQueryable();
        //    bool hasFilter = false;

        //    if (!string.IsNullOrEmpty(billingMonth))
        //    {
        //        query = query.Where(x => x.BillingMonth == billingMonth);
        //        hasFilter = true;
        //    }

        //    if (!string.IsNullOrEmpty(billingYear))
        //    {
        //        query = query.Where(x => x.BillingYear == billingYear);
        //        hasFilter = true;
        //    }

        //    if (!string.IsNullOrEmpty(block))
        //    {
        //        query = query.Where(x => x.Btno == block);
        //        hasFilter = true;
        //    }

        //    if (!string.IsNullOrEmpty(btNo))
        //    {
        //        query = query.Where(x => x.Btno == btNo);
        //        hasFilter = true;
        //    }

        //    const int pageSize = 50;
        //    var pageNumber = page ?? 1;

        //    var totalRecords = query.Count();
        //    var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        //    // Always show grid if there are records, regardless of filters
        //    ViewBag.ShowGrid = items.Any() || hasFilter || pageNumber > 1;

        //    return View(new PaginationViewModel<MaintenanceBill>
        //    {
        //        Items = items,
        //        PageNumber = pageNumber,
        //        PageSize = pageSize,
        //        TotalRecords = totalRecords
        //    });
        //}

        private List<string> GetMonths()
        {
            return new List<string> { "January", "February", "March", "April", "May", "June", "July",
                              "August", "September", "October", "November", "December" };
        }

        private List<string> GetYears()
        {
            return new List<string> { "2024", "2025" };
        }




        //[HttpGet]
        //public IActionResult MaintenanceBillsSearch(string billingMonth, string billingYear, string block, string btNo, int? page)
        //{
        //    ViewBag.Months = GetMonths();
        //    ViewBag.Years = GetYears();

        //    // Start with a join between MaintenanceBills and CustomersDetail
        //    //var query = from mb in _dbContext.MaintenanceBills
        //    //            join cd in _dbContext.CustomersDetails on mb.Btno equals cd.Btno into customerJoin
        //    //            from customer in customerJoin.DefaultIfEmpty() // Left join
        //    //            select new
        //    //            {
        //    //                MaintenanceBill = mb,
        //    //                CustomerBlock = customer.Block
        //    //            };


        //    var query = from mb in _dbContext.MaintenanceBills
        //                join cd in _dbContext.CustomersDetails on mb.Btno equals cd.Btno into customerJoin
        //                from customer in customerJoin.DefaultIfEmpty()
        //                select new MaintenanceBillViewModel  // Using ViewModel
        //                {
        //                    MaintenanceBill = mb,
        //                    Block = customer.Block
        //                };

        //    bool hasFilter = false;

        //    if (!string.IsNullOrEmpty(billingMonth))
        //    {
        //        query = query.Where(x => x.MaintenanceBill.BillingMonth == billingMonth);
        //        hasFilter = true;
        //    }

        //    if (!string.IsNullOrEmpty(billingYear))
        //    {
        //        query = query.Where(x => x.MaintenanceBill.BillingYear == billingYear);
        //        hasFilter = true;
        //    }

        //    if (!string.IsNullOrEmpty(block))
        //    {
        //        query = query.Where(x => x.Block == block);
        //        hasFilter = true;
        //    }

        //    if (!string.IsNullOrEmpty(btNo))
        //    {
        //        query = query.Where(x => x.MaintenanceBill.Btno == btNo);
        //        hasFilter = true;
        //    }

        //    const int pageSize = 50;
        //    var pageNumber = page ?? 1;

        //    // Get total count before pagination
        //    var totalRecords = query.Count();

        //    // Apply pagination and select only the MaintenanceBill entities
        //    var items = query
        //        .Skip((pageNumber - 1) * pageSize)
        //        .Take(pageSize)
        //        .Select(x => x.MaintenanceBill)
        //        .ToList();

        //    ViewBag.ShowGrid = hasFilter || pageNumber > 1;

        //    return View(new PaginationViewModel<MaintenanceBillViewModel>
        //    {
        //        Items = items,
        //        PageNumber = pageNumber,
        //        PageSize = pageSize,
        //        TotalRecords = totalRecords
        //    });
        //}


        [HttpGet]
        public IActionResult MaintenanceBillsSearch(string billingMonth, string billingYear, string block, string btNo, int? page)
        {
            ViewBag.Months = GetMonths();
            ViewBag.Years = GetYears();


               ViewBag.Blocks = _dbContext.CustomersMaintenance
                .Select(c => c.Block)
                .Where(b => !string.IsNullOrEmpty(b))
                .Distinct()
                .OrderBy(b => b)
                .ToList();
            ViewBag.SelectedBlock = block; // This comes from your action parameter


            // Check if all filter parameters are empty
            bool noFilterSelected = string.IsNullOrEmpty(billingMonth) &&
                                    string.IsNullOrEmpty(billingYear) &&
                                    string.IsNullOrEmpty(block) &&
                                    string.IsNullOrEmpty(btNo);

            // Set ViewBag message and empty grid if no filter is selected
            if (noFilterSelected)
            {
                ViewBag.Message = "Please select bill generation criteria.";
                ViewBag.ShowGrid = false;

                return View(new PaginationViewModel<MaintenanceBillViewModel>
                {
                    Items = new List<MaintenanceBillViewModel>(),
                    PageNumber = 1,
                    PageSize = 50,
                    TotalRecords = 0
                });
            }




            //var baseQuery = from mb in _dbContext.MaintenanceBills
            //                join cd in _dbContext.CustomersMaintenance on mb.Btno equals cd.Btno
            //                select new { mb, cd };

            var baseQuery = from mb in _dbContext.MaintenanceBills
                            join cm in _dbContext.CustomersMaintenance on mb.Btno equals cm.BTNo
                            select new { mb, cm };


            // Apply filters
            if (!string.IsNullOrEmpty(billingMonth))
            {
                baseQuery = baseQuery.Where(x => x.mb.BillingMonth == billingMonth);
            }

            if (!string.IsNullOrEmpty(billingYear))
            {
                baseQuery = baseQuery.Where(x => x.mb.BillingYear == billingYear);
            }

            if (!string.IsNullOrEmpty(block))
            {
                baseQuery = baseQuery.Where(x => x.cm.Block == block);
            }

            if (!string.IsNullOrEmpty(btNo))
            {
                baseQuery = baseQuery.Where(x => x.mb.Btno == btNo);
            }

            var query = baseQuery.Select(x => new MaintenanceBillViewModel
            {
                Uid = x.mb.Uid, // ✅ Make sure mb.Uid is correctly mapped
                InvoiceNo = x.mb.InvoiceNo,
                CustomerName = x.mb.CustomerName,
                Btno = x.mb.Btno,
                BillingMonth = x.mb.BillingMonth,
                BillingYear = x.mb.BillingYear,
                BillAmountInDueDate = x.mb.BillAmountInDueDate,
                BillAmountAfterDueDate = x.mb.BillAmountAfterDueDate,
                PaymentStatus = x.mb.PaymentStatus,
                Block = x.cm.Block,
                DueDate = x.mb.DueDate,
              
                //DueDate = x.mb.DueDate.HasValue
                //    ? x.mb.DueDate.Value.ToString("dd/MM/yyyy")
                //    : null // Format the DueDate as "dd/MM/yyyy"        

            });

            const int pageSize = 50;
            var pageNumber = page ?? 1;

            var totalRecords = query.Count();
            var items = query.Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .ToList();

            ViewBag.ShowGrid = items.Any() ||
                             !string.IsNullOrEmpty(billingMonth) ||
                             !string.IsNullOrEmpty(billingYear) ||
                             !string.IsNullOrEmpty(block) ||
                             !string.IsNullOrEmpty(btNo);

            return View(new PaginationViewModel<MaintenanceBillViewModel>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords
            });
        }





        public IActionResult Details(int id)
        {
            var bill = _dbContext.MaintenanceBills.FirstOrDefault(x => x.Uid == id);
            if (bill == null)
            {
                return NotFound();
            }

            return View(bill);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var bill = _dbContext.MaintenanceBills.Find(id);
            //var bill = _dbContext.MaintenanceBills.FirstOrDefault(x => x.Uid == id);
            if (bill == null)
            {
                return NotFound();
            }

            // Load Block options from CustomersMaintenance
            ViewBag.BlockList = _dbContext.CustomersMaintenance
                .Select(x => x.Block)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            return View(bill);
        }



        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Edit(int id, MaintenanceBill updatedBill)
        //{

        //    if (id != updatedBill.Uid)
        //    {
        //        return BadRequest();
        //    }

        //    if (!ModelState.IsValid)
        //    {
        //        return View(updatedBill);
        //    }

        //    var existingBill = _dbContext.MaintenanceBills.FirstOrDefault(x => x.Uid == id);
        //    if (existingBill == null)
        //    {
        //        return NotFound();
        //    }

        //    // Update properties
        //    existingBill.CustomerName = updatedBill.CustomerName;
        //    existingBill.Btno = updatedBill.Btno;
        //    existingBill.BillingMonth = updatedBill.BillingMonth;
        //    existingBill.BillingYear = updatedBill.BillingYear;
        //    existingBill.BillAmountInDueDate = updatedBill.BillAmountInDueDate;
        //    existingBill.BillAmountAfterDueDate = updatedBill.BillAmountAfterDueDate;
        //    existingBill.PaymentStatus = updatedBill.PaymentStatus;
        //    existingBill.LastUpdated = DateTime.Now;

        //    _dbContext.SaveChanges();

        //    return RedirectToAction(nameof(MaintenanceBillsSearch));
        //}


        ////Working
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(MaintenanceBill bill)
        //{
        //    if (!ModelState.IsValid)
        //        return View(bill);

        //    var existingBill = await _dbContext.MaintenanceBills.FindAsync(bill.Uid);
        //    if (existingBill == null)
        //        return NotFound();

        //    // Only update DueDate
        //    if (existingBill.DueDate != bill.DueDate)
        //    {
        //        string user = HttpContext.Session.GetString("Username") ?? "Unknown User";
        //        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        //        string newEntry = $"DueDate updated to {bill.DueDate:yyyy-MM-dd} by {user} at {timestamp}";

        //        // Append to history
        //        if (!string.IsNullOrEmpty(existingBill.History))
        //        {
        //            existingBill.History += Environment.NewLine + newEntry;
        //        }
        //        else
        //        {
        //            existingBill.History = newEntry;
        //        }

        //        existingBill.DueDate = bill.DueDate;
        //    }

        //    // Save changes
        //    await _dbContext.SaveChangesAsync();
        //    return RedirectToAction("MaintenanceBillsSearch");
        //}


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MaintenanceBill model, string action)
        {
            var bill = await _dbContext.MaintenanceBills.FindAsync(model.Uid);
            if (bill == null) return NotFound();

            string user = HttpContext.Session.GetString("Username") ?? "Unknown User";
            string timestamp = DateTime.Now.ToString("dd-MMM-yyyy HH:mm");

            if (action == "delete")
            {
                // Soft delete
                if (!bill.Btno.EndsWith("-Delete"))
                {
                    bill.Btno += "-Delete";
                    bill.BillingMonth += "-Delete";
                }

                bill.History += Environment.NewLine + $"Soft deleted by {user} on {timestamp}";

                await _dbContext.SaveChangesAsync();
                return RedirectToAction("MaintenanceBillsSearch");
            }

            if (action == "update")
            {
                if (bill.DueDate != model.DueDate)
                {
                    bill.History += Environment.NewLine + $"DueDate updated from {bill.DueDate:dd-MMM-yyyy} to {model.DueDate:dd-MMM-yyyy} by {user} on {timestamp}";
                    bill.DueDate = model.DueDate;
                }

                await _dbContext.SaveChangesAsync();
                return RedirectToAction("MaintenanceBillsSearch");
            }

            return View(model);
        }














        [Route("PrintMMultiBills")]
        [HttpGet]
        public async Task<IActionResult> PrintMMultiBills()
        {
            var projects = _dbContext.Configurations
                         .Where(c => c.ConfigKey == "Project")
                         .Select(c => c.ConfigValue)
                         .ToList();

            ViewBag.Projects = projects;
            
            return View();
        }











            [Route("PrintMMultiBills")]
        [HttpPost]
        public async Task<IActionResult> PrintMMultiBills([FromBody] PrintBillRequest request)
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

                // var url = $"http://172.20.229.3:84/api/ElectricityBill/GetEBillByUid?uids={request.uids}";

                var url = $"http://172.20.228.2:81/api/MaintenanceBill/GetMBill?category={request.category}&block={request.block}&month={request.month}&year={request.year}&project={request.project}";


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

        public IActionResult SearchBill(string? month, string? year, string? BtNo)
        {
            // If nothing is provided
            if (string.IsNullOrEmpty(BtNo) && string.IsNullOrEmpty(month) && string.IsNullOrEmpty(year))
            {
                ViewBag.ErrorMessage = "Please select a month/year or enter a BTNo.";
                return View("SearchBill");
            }

            var query = from bill in _dbContext.MaintenanceBills
                        join customer in _dbContext.CustomersDetails
                            on bill.Btno equals customer.Btno
                        select new MaintenanceBillDTO
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
                            TaxAmount = bill.TaxAmount,
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
    }
}