using BMSBT.BillServices;
using BMSBT.Models;
using BMSBT.Requests;
using BMSBT.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Data.Entity;

namespace BMSBT.Controllers
{
    public class MaintenanceNewController : Controller
    {
        private readonly BmsbtContext _dbContext;
        private readonly MaintenanceFunctions MaintenanceFunctions;
        private readonly ICurrentOperatorService _operatorService;
        public MaintenanceNewController(BmsbtContext context, ICurrentOperatorService operatorService)
        {
            _dbContext = context;
            MaintenanceFunctions = new MaintenanceFunctions(_dbContext);
            _operatorService = operatorService;
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
                    query = query.Where(c => c. BTNo.Contains(btNoSearch));
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
                PaymentStatus = x.mb.PaymentStatus,
                Block = x.cm.Block,
                DueDate = x.mb.DueDate
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



        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, MaintenanceBill updatedBill)
        {

            if (id != updatedBill.Uid)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(updatedBill);
            }

            var existingBill = _dbContext.MaintenanceBills.FirstOrDefault(x => x.Uid == id);
            if (existingBill == null)
            {
                return NotFound();
            }

            // Update properties
            existingBill.CustomerName = updatedBill.CustomerName;
            existingBill.Btno = updatedBill.Btno;
            existingBill.BillingMonth = updatedBill.BillingMonth;
            existingBill.BillingYear = updatedBill.BillingYear;
            existingBill.BillAmountInDueDate = updatedBill.BillAmountInDueDate;
            existingBill.PaymentStatus = updatedBill.PaymentStatus;
            existingBill.LastUpdated = DateTime.Now;

            _dbContext.SaveChanges();

            return RedirectToAction(nameof(MaintenanceBillsSearch));
        }






    }
}