using BMSBT.DTO;
using BMSBT.Models;
using BMSBT.Services;
using BMSBT.ViewModels;
using Microsoft.AspNetCore.Mvc;
using X.PagedList;
using X.PagedList.Extensions;

namespace BMSBT.Controllers
{
    public class SGCustomersController : Controller
    {
        private readonly BmsbtContext _dbContext;
        private const int GenerateBillPageSize = 50;

        public SGCustomersController(BmsbtContext context)
        {
            _dbContext = context;
        }

        public IActionResult Index()
        {
            return View();
        }


        public IActionResult GroupedBySubProjectAndSector()
        {
            var groupedData = _dbContext.CustomersDetails
                .GroupBy(c => new { c.Block, c.Sector })
                .Select(g => new GroupedCustomersViewModel
                {
                    SubProject = g.Key.Block,
                    Sector = g.Key.Sector,
                    Customers = g.ToList()
                }).ToList();

            return View(groupedData);
        }


        public IActionResult CategorizedBySubProject()
        {
            var groupedData = _dbContext.CustomersDetails
                .Where(c => c.BillGenerationStatus == null || c.BillGenerationStatus == "Not Generated") // Filter here
                .GroupBy(c => c.SubProject)
                .Select(g => new SubProjectCustomersViewModel
                {
                    SubProject = g.Key,
                    Customers = g.ToList()
                }).ToList();

            return View(groupedData);
        }


        public IActionResult CategorizedBySector()
        {
            var groupedData = _dbContext.CustomersDetails
                .Where(c => c.BillGenerationStatus == null || c.BillGenerationStatus == "Not Generated")
                .GroupBy(c => c.Sector)
                .Select(g => new SectorCustomersViewModel
                {
                    Sector = g.Key,
                    Customers = g.ToList()
                })
                .ToList();

            return View(groupedData);
        }




        public IActionResult CategorizedBySectorByProject(string selectedProject)
        {
            // Dropdown projects
            var projects = _dbContext.CustomersDetails
                .Select(p => p.Project)
                .Distinct()
                .ToList();

            // Start with empty result
            var filteredData = new List<SectorCustomersViewModel>();


            // Only load if project is selected
            if (!string.IsNullOrEmpty(selectedProject))
            {
                filteredData = _dbContext.CustomersDetails
                    .Where(c =>
                        (c.BillGenerationStatus == null || c.BillGenerationStatus == "Not Generated") &&
                        c.Project == selectedProject)
                    .GroupBy(c => c.Sector)
                    .Select(g => new SectorCustomersViewModel
                    {
                        Sector = g.Key,
                        Customers = g.ToList()
                    })
                    .ToList();
            }

            ViewBag.Projects = projects;
            ViewBag.SelectedProject = selectedProject;

            return View(filteredData);

        }


        public IActionResult ViewCustomer(int id)
        {
            var customer = _dbContext.CustomersDetails.FirstOrDefault(c => c.Uid == id);
            if (customer == null)
                return NotFound();

            return View(customer); // You’ll create this View next
        }





        public IActionResult GenerateBill(
            string selectedProject,
            string selectedBlock,
            string selectedStatus,
            string btNoSearch,
            int? page)
        {
            string userName = HttpContext.Session.GetString("UserName");
            string? operatorId = HttpContext.Session.GetString("OperatorId");

            // Prefer OperatorsSetup by login name (not Users.EmployeeId / wrong OperatorId)
            var operatorSetup = OperatorSetupResolver.Resolve(_dbContext, userName, operatorId);

            string? billingMonth = operatorSetup?.BillingMonth?.Trim();
            string? billingYear = operatorSetup?.BillingYear?.Trim();
            ViewBag.OperatorName = operatorSetup?.OperatorName ?? userName;
            ViewBag.BillingMonth = billingMonth;
            ViewBag.BillingYear = billingYear;

            // Keep session aligned so Generate Bill API uses the same period
            if (operatorSetup != null && !string.IsNullOrWhiteSpace(operatorSetup.OperatorID))
            {
                HttpContext.Session.SetString("OperatorId", operatorSetup.OperatorID);
                var detail = new Dictionary<string, string>
                {
                    { "OperatorId", operatorSetup.OperatorID ?? "" },
                    { "OperatorName", operatorSetup.OperatorName ?? "" },
                    { "BillingMonth", operatorSetup.BillingMonth ?? "" },
                    { "BillingYear", operatorSetup.BillingYear ?? "" }
                };
                HttpContext.Session.SetString("OperatorSetupDetail", System.Text.Json.JsonSerializer.Serialize(detail));
            }

            var projects = _dbContext.CustomersDetails
                .Where(p => p.Project != null && p.Project.Trim() != "")
                .Select(p => p.Project!.Trim())
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            var blocks = new List<string>();
            if (!string.IsNullOrWhiteSpace(selectedProject))
            {
                var trimProject = selectedProject.Trim();
                blocks = _dbContext.CustomersDetails
                    .Where(c => c.Project != null
                                && c.Project.Trim() == trimProject
                                && c.Block != null
                                && c.Block.Trim() != "")
                    .Select(c => c.Block!.Trim())
                    .Distinct()
                    .OrderBy(b => b)
                    .ToList();
            }

            // Default status for bill generation screen
            if (string.IsNullOrWhiteSpace(selectedStatus))
            {
                selectedStatus = "Not Generated";
            }

            IPagedList<CustomersDetail> pagedCustomers =
                new StaticPagedList<CustomersDetail>(Array.Empty<CustomersDetail>(), 1, GenerateBillPageSize, 0);

            if (!string.IsNullOrWhiteSpace(selectedProject)
                && !string.IsNullOrWhiteSpace(billingMonth)
                && !string.IsNullOrWhiteSpace(billingYear))
            {
                var trimProject = selectedProject.Trim();
                var query = _dbContext.CustomersDetails
                    .Where(c => c.Project != null && c.Project.Trim() == trimProject);

                if (!string.IsNullOrWhiteSpace(selectedBlock))
                {
                    var trimBlock = selectedBlock.Trim();
                    query = query.Where(c => c.Block != null && c.Block.Trim() == trimBlock);
                }

                if (!string.IsNullOrWhiteSpace(btNoSearch))
                {
                    var term = btNoSearch.Trim();
                    query = query.Where(c => c.Btno != null && c.Btno.Contains(term));
                }

                // Generation status for operator billing month/year via EBill_Comparison (testing)
                var billedBtNos = _dbContext.EBillComparisons
                    .Where(b => b.BillingMonth == billingMonth
                                && b.BillingYear == billingYear
                                && b.Btno != null)
                    .Select(b => b.Btno!)
                    .Distinct();

                if (string.Equals(selectedStatus, "Generated", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(c => c.Btno != null && billedBtNos.Contains(c.Btno));
                }
                else if (string.Equals(selectedStatus, "Not Generated", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(c => c.Btno == null || !billedBtNos.Contains(c.Btno));
                }

                int pageNumber = page ?? 1;
                pagedCustomers = query
                    .OrderBy(c => c.Block)
                    .ThenBy(c => c.Btno)
                    .ToPagedList(pageNumber, GenerateBillPageSize);
            }

            ViewBag.Projects = projects;
            ViewBag.Blocks = blocks;
            ViewBag.SelectedProject = selectedProject;
            ViewBag.SelectedBlock = selectedBlock;
            ViewBag.SelectedStatus = selectedStatus;
            ViewBag.BTNoSearch = btNoSearch;
            ViewBag.Statuses = new List<string> { "Not Generated", "Generated" };

            return View(pagedCustomers);
        }

        [HttpGet]
        public JsonResult GetBlocksByProject(string project)
        {
            if (string.IsNullOrWhiteSpace(project))
            {
                return Json(new List<string>());
            }

            var trimProject = project.Trim();
            var blocks = _dbContext.CustomersDetails
                .Where(c => c.Project != null
                            && c.Project.Trim() == trimProject
                            && c.Block != null
                            && c.Block.Trim() != "")
                .Select(c => c.Block!.Trim())
                .Distinct()
                .OrderBy(b => b)
                .ToList();

            return Json(blocks);
        }




        public IActionResult ViewBillsByYear(string selectedYear, string selectedMonth)
        {
           var years = _dbContext.ElectricityBills
          .Where(b => b.BillingYear != null)
          .Select(b => b.BillingYear.Trim())
          .Distinct()
          .OrderByDescending(y => y)
          .ToList();

         var months = _dbContext.ElectricityBills
        .Where(b => b.BillingMonth != null)
        .Select(b => b.BillingMonth.Trim())
        .Distinct()
        .OrderBy(m => m)
        .ToList();

            var filteredData = new List<BillsViewModel>();



            // Load data only if both filters are selected
            if (!string.IsNullOrEmpty(selectedYear) && !string.IsNullOrEmpty(selectedMonth))
            {
                var query = _dbContext.ElectricityBills
                    .Where(b => b.BillingYear == selectedYear && b.BillingMonth == selectedMonth && b.Sector != null);

                filteredData = query
                    .GroupBy(b => b.Sector.Trim())
                    .Select(g => new BillsViewModel
                    {
                        Sector = g.Key,
                        Bills = g.ToList()
                    })
                    .OrderBy(g => g.Sector)
                    .ToList();
            }

            ViewBag.Years = years;
            ViewBag.Months = months;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedMonth = selectedMonth;

            return View(filteredData);
        }





    }
}
