using BMSBT.Models;
using BMSBT.Roles;
using BMSBT.ViewModels;
using DevExpress.ClipboardSource.SpreadsheetML;
using DevExpress.CodeParser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;
using static DevExpress.XtraPrinting.Native.PageSizeInfo;

namespace BMSBT.Controllers
{
  
    public class CustomersController : Controller
    {
        private readonly BmsbtContext _context;
        public CustomersController(BmsbtContext context)
        {
            _context = context;

        }





        // GET: Customers
        public async Task<IActionResult> Index(string searchString, string sortOrder, int pageNumber = 1, int pageSize = 10)
        {
            // Session Check
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }

            ViewBag.Username = HttpContext.Session.GetString("UserName");
            ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");

            // Dashboard Statistics - Grouping by 'Project' column
            var projectData = await _context.CustomersDetails
                .GroupBy(c => c.Project)
                .Select(g => new
                {
                    ProjectName = g.Key,
                    TotalCustomers = g.Count()
                })
                .ToListAsync();

            // Extract labels and data for chart
            List<string> labels = projectData.Select(x => x.ProjectName).ToList();
            List<int> data = projectData.Select(x => x.TotalCustomers).ToList();

            ViewBag.ChartLabels = labels;
            ViewBag.ChartData = data;

            // Grid Functionality
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentSort"] = sortOrder;
            ViewData["BtnoSortParm"] = String.IsNullOrEmpty(sortOrder) ? "btno_desc" : "";
            ViewData["NameSortParm"] = sortOrder == "name" ? "name_desc" : "name";
            ViewData["ProjectSortParm"] = sortOrder == "project" ? "project_desc" : "project";

            var customers = from c in _context.CustomersDetails
                            select c;

            // Search Filter
            if (!string.IsNullOrEmpty(searchString))
            {
                customers = customers.Where(s =>
                    (s.CustomerNo != null && s.CustomerNo.Contains(searchString)) ||
                    (s.CustomerName != null && s.CustomerName.Contains(searchString)) ||
                    (s.Btno != null && s.Btno.Contains(searchString)) ||
                    (s.Cnicno != null && s.Cnicno.Contains(searchString)) ||
                    (s.MobileNo != null && s.MobileNo.Contains(searchString)));
            }

            // Sorting
            customers = sortOrder switch
            {
                "btno_desc" => customers.OrderByDescending(c => c.Btno),
                "name" => customers.OrderBy(c => c.CustomerName),
                "name_desc" => customers.OrderByDescending(c => c.CustomerName),
                "project" => customers.OrderBy(c => c.Project),
                "project_desc" => customers.OrderByDescending(c => c.Project),
                _ => customers.OrderBy(c => c.Btno),
            };

            // Pagination
            var totalCount = await customers.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRecords = totalCount;

            var customersList = await customers
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(customersList);
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
            if (HttpContext.Session.GetString("UserName") == null)
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
            if (HttpContext.Session.GetString("UserName") == null)
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










        // GET: Customers/CustomersSelection
        public async Task<IActionResult> CustomersSelection()
        {
            // Session Check
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }

            ViewBag.Username = HttpContext.Session.GetString("UserName");
            ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");

            try
            {
                // Get all projects from Configurations table where ConfigKey = "Project" or "Projects"
                var projects = await _context.Configurations
                    .Where(c => c.ConfigKey == "Project" || c.ConfigKey == "Projects")
                    .Select(c => c.ConfigValue)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToListAsync();

                // Get blocks from both configuration keys
                var mohlanwalBlocks = await _context.Configurations
                    .Where(c => c.ConfigKey == "BlockMohlanwal")
                    .Select(c => c.ConfigValue)
                    .Distinct()
                    .ToListAsync();

                var orchardsBlocks = await _context.Configurations
                    .Where(c => c.ConfigKey == "BlockOrchards")
                    .Select(c => c.ConfigValue)
                    .Distinct()
                    .ToListAsync();

                // Combine all blocks
                var allBlocks = new List<string>();
                allBlocks.AddRange(mohlanwalBlocks ?? new List<string>());
                allBlocks.AddRange(orchardsBlocks ?? new List<string>());
                var blocks = allBlocks.Distinct().OrderBy(b => b).ToList();

                // Get plot types from Configurations table where ConfigKey = "PlotType"
                var plotTypes = await _context.Configurations
                    .Where(c => c.ConfigKey == "PlotType")
                    .Select(c => c.ConfigValue)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToListAsync();

                ViewBag.Projects = projects ?? new List<string>();
                ViewBag.Blocks = blocks ?? new List<string>();
                ViewBag.Categories = plotTypes ?? new List<string>();
                ViewBag.MohlanwalBlocks = mohlanwalBlocks ?? new List<string>();
                ViewBag.OrchardsBlocks = orchardsBlocks ?? new List<string>();

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error loading selection data: " + ex.Message;
                return View();
            }
        }

        // POST: Customers/GetCustomersBySelection
        [HttpPost]
        public async Task<IActionResult> GetCustomersBySelection(string project, string block, string category)
        {
            // Session Check
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            try
            {
                var query = _context.CustomersDetails.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(project) && project != "All")
                    query = query.Where(c => c.Project == project);

                if (!string.IsNullOrEmpty(block) && block != "All")
                    query = query.Where(c => c.Block == block);

                if (!string.IsNullOrEmpty(category) && category != "All")
                    query = query.Where(c => c.PlotType == category);

                var totalRecords = await query.CountAsync();

                return Json(new
                {
                    success = true,
                    totalRecords = totalRecords,
                    message = $"Found {totalRecords} customer(s) matching the criteria"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: Customers/GetAllCustomersBySelection
        [HttpPost]
        public async Task<IActionResult> GetAllCustomersBySelection(string project, string block, string category)
        {
            // Session Check
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            try
            {
                // Start with base query
                var query = _context.CustomersDetails.AsQueryable();

                // Apply filters if provided
                if (!string.IsNullOrEmpty(project) && project != "All")
                {
                    query = query.Where(c => c.Project == project);
                }

                if (!string.IsNullOrEmpty(block) && block != "All")
                {
                    query = query.Where(c => c.Block == block);
                }

                if (!string.IsNullOrEmpty(category) && category != "All")
                {
                    query = query.Where(c => c.Category == category);
                }

                // Get all customer details
                var customerDetails = await query
                    .Select(c => new CustomerDetailResult
                    {
                        CustomerNo = c.CustomerNo,
                        CustomerName = c.CustomerName,
                        CNICNo = c.Cnicno,
                        MobileNo = c.MobileNo,
                        City = c.City,
                        Sector = c.Sector,
                        Block = c.Block,
                        PlotNo = c.PloNo,
                        Project = c.Project,
                        Category = c.Category
                    })
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    customerDetails = customerDetails,
                    message = $"Retrieved {customerDetails.Count} customer(s)"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Error: {ex.Message}"
                });
            }
        }




















    }
}
