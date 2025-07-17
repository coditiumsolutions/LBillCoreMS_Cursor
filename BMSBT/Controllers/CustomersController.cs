using BMSBT.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using X.PagedList.Extensions;
using static DevExpress.XtraPrinting.Native.PageSizeInfo;
using DevExpress.CodeParser;
using DevExpress.ClipboardSource.SpreadsheetML;
using Microsoft.AspNetCore.Authorization;
using BMSBT.Roles;

namespace BMSBT.Controllers
{
  
    public class CustomersController : Controller
    {
        private readonly BmsbtContext _context;
        public CustomersController(BmsbtContext context)
        {
            _context = context;

        }

        //[CustomAuthorize("BillOfficer,BillManager")]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.Username = HttpContext.Session.GetString("UserName");
            ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");


            // Grouping by 'Project' column and counting the total customers for each project
            var projectData = _context.CustomersDetails
                .GroupBy(c => c.Project) // Group by Project column
                .Select(g => new
                {
                    ProjectName = g.Key, // Project Name
                    TotalCustomers = g.Count() // Renaming Count to TotalCustomers
                })
                .ToList();

            // Extracting labels (Project names) and data (Total customers per project)
            List<string> labels = projectData.Select(x => x.ProjectName).ToList();
            List<int> data = projectData.Select(x => x.TotalCustomers).ToList();

            // Passing data to View
            ViewBag.ChartLabels = labels;
            ViewBag.ChartData = data;

            //ViewBag.TotalCustomers = totalCustomers; // Send total customers count

            return View();
        }
        
        public IActionResult SearchCustomers()
        {
            return View();
        }

        public IActionResult SearchAll(string search)
        {
            // If search is empty, return an empty list
            if (string.IsNullOrEmpty(search))
            {
                return View(new List<CustomersDetail>());
            }

            var customers = _context.CustomersDetails
                .Where(c => (c.Btno != null && c.Btno.Contains(search)) ||
                            (c.CustomerName != null && c.CustomerName.Contains(search)))
                .ToList();

            return View(customers);
        }






        public IActionResult AllCustomers(string project, string sector, string block, int? page)
        {
            if (HttpContext.Session.GetInt32("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.Username = HttpContext.Session.GetString("UserName");
            ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");

            // Populate dropdown data
            ViewBag.Projects = _context.Configurations
                                 .Where(c => c.ConfigKey == "Project")
                                 .Select(c => c.ConfigValue)
                                 .ToList();


            var Sectors = _context.Configurations
                                   .Where(c => c.ConfigKey == project)
                                   .Select(c => c.ConfigValue)
                                   .ToList();
            ViewBag.Sectors = Sectors;

            // Get all sectors (assuming the field is "Sector" in your database)
            ViewBag.Blocks = _context.Configurations
                                  .Where(c => c.ConfigKey == "Block" + project)
                                  .Select(c => c.ConfigValue)
                                  .ToList();

            ViewBag.Tarrif = _context.Tarrifs.Select(t => new { t.Uid, t.TarrifName }).ToList();

            // Apply filters
            var query = _context.CustomersDetails.AsQueryable();

            if (!string.IsNullOrEmpty(project))
                query = query.Where(x => x.Project == project);

            if (!string.IsNullOrEmpty(sector))
                query = query.Where(x => x.Sector == sector);

            if (!string.IsNullOrEmpty(block))
                query = query.Where(x => x.Block == block);

            // Total Records Count
            ViewBag.TotalRecords = query.Count();
            // Calculate total records by category
            ViewBag.TotalRecordsByProject = _context.CustomersDetails.Count(x => x.Project == project);
            ViewBag.TotalRecordsBySector = _context.CustomersDetails.Count(x => x.Sector == sector);
            ViewBag.TotalRecordsByBlock = _context.CustomersDetails.Count(x => x.Block == block);




            int pageNumber = page ?? 1;
            int pageSize = 500;

            return View(query.ToPagedList(pageNumber, pageSize));
        }




        public IActionResult AllCustomersBySector(string project, string sector, string block, int? page)
        {
            if (HttpContext.Session.GetInt32("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.Username = HttpContext.Session.GetString("UserName");
            ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");
            
            // Populate dropdown data
            ViewBag.Projects = _context.Configurations
                                 .Where(c => c.ConfigKey == "Project")
                                 .Select(c => c.ConfigValue)
                                 .ToList();

            var Sectors = _context.Configurations
                                   .Where(c => c.ConfigKey == project)
                                   .Select(c => c.ConfigValue)
                                   .ToList();
            ViewBag.Sectors = Sectors;


            // Get all sectors (assuming the field is "Sector" in your database)
            ViewBag.Blocks = _context.Configurations
                                  .Where(c => c.ConfigKey == "Block" + project)
                                  .Select(c => c.ConfigValue)
                                  .ToList();


            ViewBag.Tarrif = _context.Tarrifs.Select(t => new { t.Uid, t.TarrifName }).ToList();

            // Apply filters
            var query = _context.CustomersDetails.AsQueryable();

            if (!string.IsNullOrEmpty(project))
                query = query.Where(x => x.Project == project);

            if (!string.IsNullOrEmpty(sector))
                query = query.Where(x => x.Sector == sector);

            if (!string.IsNullOrEmpty(block))
                query = query.Where(x => x.Block == block);




            // Total Records Count
            ViewBag.TotalRecords = query.Count();
            // Calculate total records by category
            ViewBag.TotalRecordsByProject = _context.CustomersDetails.Count(x => x.Project == project);
            ViewBag.TotalRecordsBySector = _context.CustomersDetails.Count(x => x.Sector == sector);
            ViewBag.TotalRecordsByBlock = _context.CustomersDetails.Count(x => x.Block == block);




            int pageNumber = page ?? 1;
            int pageSize = 500;

            return View(query.ToPagedList(pageNumber, pageSize));
        }




        // AJAX endpoint for cascading dropdowns
        public JsonResult GetSubprojects(string project)
        {
            if (string.IsNullOrEmpty(project))
                return Json(new List<string>());


            var SubProjects = _context.Configurations
                                 .Where(c => c.ConfigKey == project)
                                 .Select(c => c.ConfigValue)
                                 .ToList();
            return Json(SubProjects);
        }


        public IActionResult SelectionGrid()
        {
           return View();
        }

        public IActionResult CustomersDetail()
        {
            return View();
        }


        public IActionResult GetCustomersPSPT(string project, string subproject, string tariffname)
        {
            var customers = string.IsNullOrEmpty(tariffname)
             ? _context.CustomersDetails
                       .Where(c => c.Project == project && c.SubProject == subproject)
                       .ToList()
             : _context.CustomersDetails
                       .Where(c => c.Project == project && c.SubProject == subproject && c.TariffName == tariffname)
                       .ToList();
            return PartialView("_CustomerGrid", customers);
        }



        public IActionResult GetCustomersPSPS(string project, string subproject, string sectorname)
        {
            var customers = string.IsNullOrEmpty(sectorname)
             ? _context.CustomersDetails
                       .Where(c => c.Project == project && c.SubProject == subproject)
                       .ToList()
             : _context.CustomersDetails
                       .Where(c => c.Project == project && c.SubProject == subproject && c.Sector == sectorname)
                       .ToList();
            return PartialView("_CustomerGrid", customers);
        }



       
        public IActionResult Report()
        {
            List<string> labels = new List<string> { "January", "February", "March", "April" };
            List<int> data = new List<int> { 40, 60, 80, 100 };
            if (labels == null || data == null)
            {
                return BadRequest("Chart data is missing.");
            }
            ViewBag.ChartLabels = labels;
            ViewBag.ChartData = data;
            return View();
        }

        public IActionResult GraphReportPSB()
        {
            // Grouping by 'Project' column and counting the total customers for each project
            var projectData = _context.CustomersDetails
                .GroupBy(c => c.Project) // Group by Project column
                .Select(g => new
                {
                    ProjectName = g.Key, // Project Name
                    TotalCustomers = g.Count() // Renaming Count to TotalCustomers
                })
                .ToList();

            // Extracting labels (Project names) and data (Total customers per project)
            List<string> labels = projectData.Select(x => x.ProjectName).ToList();
            List<int> data = projectData.Select(x => x.TotalCustomers).ToList();

            // Passing data to View
            ViewBag.ChartLabels = labels;
            ViewBag.ChartData = data;

            //ViewBag.TotalCustomers = totalCustomers; // Send total customers count

            return View();      
        }



        public IActionResult Details(int id)
        {
            var customer = _context.CustomersDetails.FirstOrDefault(c => c.Uid == id);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }


        [HttpGet]
        public IActionResult Edit(int id)
        {
            var customer = _context.CustomersDetails.FirstOrDefault(c => c.Uid == id);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        [HttpPost]
        public IActionResult Edit(CustomersDetail customer)
        {
            if (ModelState.IsValid)
            {
                var existingCustomer = _context.CustomersDetails.Find(customer.Uid);
                if (existingCustomer != null)
                {
                    // Update fields manually
                   // existingCustomer.Btno = customer.Btno;
                    existingCustomer.CustomerName = customer.CustomerName;
                    existingCustomer.Project = customer.Project;
                    existingCustomer.Block = customer.Block;
                    existingCustomer.Sector = customer.Sector;
                    existingCustomer.PloNo = customer.PloNo;
                    existingCustomer.Category = customer.Category;
                    existingCustomer.CustomerNo = customer.CustomerNo;
                    existingCustomer.SubProject = customer.SubProject;
                    existingCustomer.TariffName = customer.TariffName;

                    _context.Update(existingCustomer);
                    _context.SaveChanges();
                    return RedirectToAction("SearchAll");
                }
            }
            return View(customer);
        }




    }
}
