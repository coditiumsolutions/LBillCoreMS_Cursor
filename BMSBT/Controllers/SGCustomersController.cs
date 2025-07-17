using BMSBT.DTO;
using BMSBT.Models;
using BMSBT.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BMSBT.Controllers
{
    public class SGCustomersController : Controller
    {
        private readonly BmsbtContext _dbContext;

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





        public IActionResult GenerateBill(string selectedProject, string btNoSearch)
        {
            // Dropdown projects
            var projects = _dbContext.CustomersDetails
                .Select(p => p.Project.Trim())
                .Distinct()
                .ToList();


           

            // Start with empty result
            var filteredData = new List<SectorCustomersViewModel>();

            // Only load if project is selected
            if (!string.IsNullOrEmpty(selectedProject))
            {

                var query = _dbContext.CustomersDetails
            .Where(c =>
                (c.BillGenerationStatus == null || c.BillGenerationStatus == "Not Generated") &&
                c.Project.Trim() == selectedProject.Trim());

                if (!string.IsNullOrEmpty(btNoSearch))
                {
                    query = query.Where(c => c.Btno.Contains(btNoSearch));
                }

            filteredData = query
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
