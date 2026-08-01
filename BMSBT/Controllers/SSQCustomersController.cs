// Controllers/SSQCustomersController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BMSBT.Models;
using BMSBT.Services;
using BMSBT.ViewModels;

using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using X.PagedList.Extensions;

namespace BMSBT.Controllers
{
    public class SSQCustomersController : Controller
    {
        private const string ECustomerSearchSessionKey = "SSQ_ECustomerSearch";
        private const string MCustomerSearchSessionKey = "SSQ_MCustomerSearch";
        private const int CustomerPageSize = 20;

        private readonly BmsbtContext _context;
        private readonly IAuditLogService _auditLogService;

        public SSQCustomersController(BmsbtContext context, IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        // GET: SSQCustomers
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }

            ViewBag.Username = HttpContext.Session.GetString("UserName");
            ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");
            return View();
        }

        public async Task<IActionResult> List(string searchString, string sortOrder, int page = 1, int pageSize = 10)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["BtnoSortParm"] = sortOrder == "btno" ? "btno_desc" : "btno"; // CHANGED: CustomerNoSortParm to BtnoSortParm
            ViewData["CurrentFilter"] = searchString;

            var customers = from c in _context.CustomersDetails
                            select c;

            if (!string.IsNullOrEmpty(searchString))
            {
                customers = customers.Where(c =>
                    (c.Btno != null && c.Btno.Contains(searchString)) ||      // CHANGED: c.CustomerNo to c.Btno
                    (c.CustomerName != null && c.CustomerName.Contains(searchString)) ||
                    (c.Cnicno != null && c.Cnicno.Contains(searchString)) ||
                    (c.MobileNo != null && c.MobileNo.Contains(searchString)) ||
                    (c.City != null && c.City.Contains(searchString)) ||
                    (c.Sector != null && c.Sector.Contains(searchString)) ||
                    (c.Block != null && c.Block.Contains(searchString)) ||
                    (c.PloNo != null && c.PloNo.Contains(searchString)));
            }

            switch (sortOrder)
            {
                case "name_desc":
                    customers = customers.OrderByDescending(c => c.CustomerName);
                    break;
                case "btno":                                      // CHANGED: "customerNo" to "btno"
                    customers = customers.OrderBy(c => c.Btno);   // CHANGED: c.CustomerNo to c.Btno
                    break;
                case "btno_desc":                                 // CHANGED: "customerNo_desc" to "btno_desc"
                    customers = customers.OrderByDescending(c => c.Btno); // CHANGED: c.CustomerNo to c.Btno
                    break;
                default:
                    customers = customers.OrderBy(c => c.Uid);
                    break;
            }

            var totalRecords = await customers.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var paginatedCustomers = await customers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;
            ViewData["PageSize"] = pageSize;

            return View(paginatedCustomers);
        }

        // GET: SSQCustomers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            var redirect = RequireLogin();
            if (redirect != null)
            {
                return redirect;
            }

            if (id == null)
            {
                return NotFound();
            }

            var customersDetail = await _context.CustomersDetails
                .FirstOrDefaultAsync(m => m.Uid == id);

            if (customersDetail == null)
            {
                return NotFound();
            }

            return View(customersDetail);
        }

        // GET: SSQCustomers/Create
        public IActionResult Create()
        {
            var redirect = RequireLogin();
            if (redirect != null)
            {
                return redirect;
            }

            return View();
        }

        // POST: SSQCustomers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CustomerNo,Btno,CustomerName,GeneratedMonthYear,LocationSeqNo,Cnicno,FatherName,InstalledOn,MobileNo,TelephoneNo,MeterType,Ntnnumber,City,Project,SubProject,TariffName,BankNo,BtnoMaintenance,Category,Block,PlotType,Size,Sector,PloNo,BillStatusMaint,BillStatus,BillGenerationStatus,History,MeterNo")] CustomersDetail customersDetail)
        {
            if (ModelState.IsValid)
            {
                _context.Add(customersDetail);
                HttpContext.Items["SkipEfAudit"] = true;
                try
                {
                    await _context.SaveChangesAsync();
                }
                finally
                {
                    HttpContext.Items.Remove("SkipEfAudit");
                }

                await _auditLogService.LogAsync(
                    CustomerAuditHelper.ECustomerTable,
                    "INSERT",
                    CustomerAuditHelper.GetElectricityRecordId(customersDetail),
                    null,
                    CustomerAuditHelper.CreateElectricitySnapshot(customersDetail),
                    CustomerAuditHelper.ECustomerModule);

                TempData["SuccessMessage"] = "Customer created successfully!";
                return RedirectToAction(nameof(ECustomers));
            }
            return View(customersDetail);
        }

        // GET: SSQCustomers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var redirect = RequireLogin();
            if (redirect != null)
            {
                return redirect;
            }

            if (id == null)
            {
                return NotFound();
            }

            var customersDetail = await _context.CustomersDetails.FindAsync(id);
            if (customersDetail == null)
            {
                return NotFound();
            }
            return View(customersDetail);
        }

        // POST: SSQCustomers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Uid,CustomerNo,Btno,CustomerName,GeneratedMonthYear,LocationSeqNo,Cnicno,FatherName,InstalledOn,MobileNo,TelephoneNo,MeterType,Ntnnumber,City,Project,SubProject,TariffName,BankNo,BtnoMaintenance,Category,Block,PlotType,Size,Sector,PloNo,BillStatusMaint,BillStatus,BillGenerationStatus,History,MeterNo")] CustomersDetail customersDetail)
        {
            if (id != customersDetail.Uid)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.CustomersDetails.FindAsync(id);
                    if (existing == null)
                    {
                        return NotFound();
                    }

                    var oldSnapshot = CustomerAuditHelper.CreateElectricitySnapshot(existing);

                    existing.CustomerNo = customersDetail.CustomerNo;
                    existing.Btno = customersDetail.Btno;
                    existing.CustomerName = customersDetail.CustomerName;
                    existing.GeneratedMonthYear = customersDetail.GeneratedMonthYear;
                    existing.LocationSeqNo = customersDetail.LocationSeqNo;
                    existing.Cnicno = customersDetail.Cnicno;
                    existing.FatherName = customersDetail.FatherName;
                    existing.InstalledOn = customersDetail.InstalledOn;
                    existing.MobileNo = customersDetail.MobileNo;
                    existing.TelephoneNo = customersDetail.TelephoneNo;
                    existing.MeterType = customersDetail.MeterType;
                    existing.Ntnnumber = customersDetail.Ntnnumber;
                    existing.City = customersDetail.City;
                    existing.Project = customersDetail.Project;
                    existing.SubProject = customersDetail.SubProject;
                    existing.TariffName = customersDetail.TariffName;
                    existing.BankNo = customersDetail.BankNo;
                    existing.BtnoMaintenance = customersDetail.BtnoMaintenance;
                    existing.Category = customersDetail.Category;
                    existing.Block = customersDetail.Block;
                    existing.PlotType = customersDetail.PlotType;
                    existing.Size = customersDetail.Size;
                    existing.Sector = customersDetail.Sector;
                    existing.PloNo = customersDetail.PloNo;
                    existing.BillStatusMaint = customersDetail.BillStatusMaint;
                    existing.BillStatus = customersDetail.BillStatus;
                    existing.BillGenerationStatus = customersDetail.BillGenerationStatus;
                    existing.History = customersDetail.History;
                    existing.MeterNo = customersDetail.MeterNo;

                    var newSnapshot = CustomerAuditHelper.CreateElectricitySnapshot(existing);
                    var (oldData, newData) = AuditDiffHelper.BuildDiff(oldSnapshot, newSnapshot);

                    HttpContext.Items["SkipEfAudit"] = true;
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    finally
                    {
                        HttpContext.Items.Remove("SkipEfAudit");
                    }

                    if (oldData.Count > 0)
                    {
                        await _auditLogService.LogAsync(
                            CustomerAuditHelper.ECustomerTable,
                            "UPDATE",
                            CustomerAuditHelper.GetElectricityRecordId(existing),
                            oldData,
                            newData,
                            CustomerAuditHelper.ECustomerModule);
                    }

                    TempData["SuccessMessage"] = "Customer updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomersDetailExists(customersDetail.Uid))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ECustomers));
            }
            return View(customersDetail);
        }

        // GET: SSQCustomers/Delete/5
        //public async Task<IActionResult> Delete(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var customersDetail = await _context.CustomersDetails
        //        .FirstOrDefaultAsync(m => m.Uid == id);

        //    if (customersDetail == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(customersDetail);
        //}

        //// POST: SSQCustomers/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteConfirmed(int id)
        //{
        //    var customersDetail = await _context.CustomersDetails.FindAsync(id);
        //    if (customersDetail != null)
        //    {
        //        _context.CustomersDetails.Remove(customersDetail);
        //        await _context.SaveChangesAsync();
        //        TempData["SuccessMessage"] = "Customer deleted successfully!";
        //    }
        //    return RedirectToAction(nameof(Index));
        //}

        private IActionResult? RequireLogin()
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }

            ViewBag.Username = HttpContext.Session.GetString("UserName");
            ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");
            return null;
        }

        private List<string> GetProjectOptions()
        {
            return _context.Configurations
                .AsNoTracking()
                .Where(c => c.ConfigKey != null &&
                            c.ConfigValue != null &&
                            (c.ConfigKey.Trim().ToLower() == "project" ||
                             c.ConfigKey.Trim().ToLower() == "projects") &&
                            c.ConfigValue.Trim() != "")
                .Select(c => c.ConfigValue!.Trim())
                .Distinct()
                .OrderBy(p => p)
                .ToList();
        }

        private CustomerSearchState GetSavedSearchState(string sessionKey)
        {
            var json = HttpContext.Session.GetString(sessionKey);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new CustomerSearchState();
            }

            return JsonSerializer.Deserialize<CustomerSearchState>(json) ?? new CustomerSearchState();
        }

        private void SaveSearchState(string sessionKey, string? project, string? sector, string? btNo, int? page, string? block = null)
        {
            var state = new CustomerSearchState
            {
                Project = project,
                Sector = sector,
                Block = block,
                BtNo = btNo,
                Page = page ?? 1
            };

            HttpContext.Session.SetString(sessionKey, JsonSerializer.Serialize(state));
        }

        private List<string> GetEBlocks(string? project)
        {
            var query = _context.CustomersDetails.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(project))
            {
                query = query.Where(c => c.Project == project);
            }

            return query
                .Select(c => c.Block)
                .Where(b => b != null && b != "")
                .Distinct()
                .OrderBy(b => b)
                .ToList()!;
        }

        private List<string> GetMSectors(string? project)
        {
            var query = _context.CustomersMaintenance.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(project))
            {
                query = query.Where(c => c.Project == project);
            }

            return query
                .Select(c => c.Sector)
                .Where(s => s != null && s != "")
                .Distinct()
                .OrderBy(s => s)
                .ToList()!;
        }

        private IQueryable<CustomersDetail> BuildECustomersQuery(string? project, string? block, string? btNo)
        {
            var query = _context.CustomersDetails.AsQueryable();

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

            return query
                .OrderBy(c => c.Project)
                .ThenBy(c => c.Block)
                .ThenBy(c => c.Btno);
        }

        private IQueryable<CustomersMaintenance> BuildMCustomersQuery(string? project, string? sector, string? btNo)
        {
            var query = _context.CustomersMaintenance.AsQueryable();

            if (!string.IsNullOrWhiteSpace(project))
            {
                query = query.Where(c => c.Project == project);
            }

            if (!string.IsNullOrWhiteSpace(sector))
            {
                query = query.Where(c => c.Sector == sector);
            }

            if (!string.IsNullOrWhiteSpace(btNo))
            {
                var term = btNo.Trim();
                query = query.Where(c =>
                    (c.BTNo != null && c.BTNo.Contains(term)) ||
                    (c.PloNo != null && c.PloNo.Contains(term)));
            }

            return query
                .OrderBy(c => c.Project)
                .ThenBy(c => c.Sector)
                .ThenBy(c => c.BTNo);
        }

        public IActionResult ECustomers()
        {
            var redirect = RequireLogin();
            if (redirect != null)
            {
                return redirect;
            }

            var saved = GetSavedSearchState(ECustomerSearchSessionKey);
            var model = new SSQECustomerFilterViewModel
            {
                Projects = GetProjectOptions(),
                Blocks = GetEBlocks(saved.Project),
                SelectedProject = saved.Project,
                SelectedBlock = saved.Block,
                SearchBtNo = saved.BtNo,
                CurrentPage = saved.Page,
                Customers = saved.HasFilters
                    ? BuildECustomersQuery(saved.Project, saved.Block, saved.BtNo).ToPagedList(saved.Page, CustomerPageSize)
                    : new List<CustomersDetail>().ToPagedList(1, CustomerPageSize)
            };

            return View(model);
        }

        [HttpGet]
        public JsonResult GetEBlocksByProject(string project)
        {
            return Json(GetEBlocks(project));
        }

        [HttpGet]
        public PartialViewResult FilterECustomers(string project, string block, string btNo, int? page)
        {
            SaveSearchState(ECustomerSearchSessionKey, project, null, btNo, page, block);

            var pageNumber = page ?? 1;
            var customers = BuildECustomersQuery(project, block, btNo)
                .ToPagedList(pageNumber, CustomerPageSize);

            return PartialView("_ECustomersGrid", customers);
        }

        public IActionResult MCustomers()
        {
            var redirect = RequireLogin();
            if (redirect != null)
            {
                return redirect;
            }

            var saved = GetSavedSearchState(MCustomerSearchSessionKey);
            var model = new SSQMCustomerFilterViewModel
            {
                Projects = GetProjectOptions(),
                Sectors = GetMSectors(saved.Project),
                SelectedProject = saved.Project,
                SelectedSector = saved.Sector,
                SearchBtNo = saved.BtNo,
                CurrentPage = saved.Page,
                Customers = saved.HasFilters
                    ? BuildMCustomersQuery(saved.Project, saved.Sector, saved.BtNo).ToPagedList(saved.Page, CustomerPageSize)
                    : new List<CustomersMaintenance>().ToPagedList(1, CustomerPageSize)
            };

            return View(model);
        }

        [HttpGet]
        public JsonResult GetMSectorsByProject(string project)
        {
            return Json(GetMSectors(project));
        }

        [HttpGet]
        public PartialViewResult FilterMCustomers(string project, string sector, string btNo, int? page)
        {
            SaveSearchState(MCustomerSearchSessionKey, project, sector, btNo, page);

            var pageNumber = page ?? 1;
            var customers = BuildMCustomersQuery(project, sector, btNo)
                .ToPagedList(pageNumber, CustomerPageSize);

            return PartialView("_MCustomersGrid", customers);
        }

        public async Task<IActionResult> MDetails(int? id)
        {
            var redirect = RequireLogin();
            if (redirect != null)
            {
                return redirect;
            }

            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.CustomersMaintenance.FirstOrDefaultAsync(m => m.Uid == id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        public IActionResult MCreate()
        {
            var redirect = RequireLogin();
            if (redirect != null)
            {
                return redirect;
            }

            return View(new CustomersMaintenance
            {
                CustomerNo = string.Empty,
                Project = string.Empty,
                SubProject = string.Empty,
                TariffName = string.Empty,
                Category = string.Empty,
                Block = string.Empty,
                Sector = string.Empty,
                PloNo = string.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MCreate(CustomersMaintenance customer)
        {
            var redirect = RequireLogin();
            if (redirect != null)
            {
                return redirect;
            }

            if (ModelState.IsValid)
            {
                _context.Add(customer);
                HttpContext.Items["SkipEfAudit"] = true;
                try
                {
                    await _context.SaveChangesAsync();
                }
                finally
                {
                    HttpContext.Items.Remove("SkipEfAudit");
                }

                await _auditLogService.LogAsync(
                    CustomerAuditHelper.MCustomerTable,
                    "INSERT",
                    CustomerAuditHelper.GetMaintenanceRecordId(customer),
                    null,
                    CustomerAuditHelper.CreateMaintenanceSnapshot(customer),
                    CustomerAuditHelper.MCustomerModule);

                TempData["SuccessMessage"] = "Maintenance customer created successfully!";
                return RedirectToAction(nameof(MCustomers));
            }

            return View(customer);
        }

        public async Task<IActionResult> MEdit(int? id)
        {
            var redirect = RequireLogin();
            if (redirect != null)
            {
                return redirect;
            }

            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.CustomersMaintenance.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MEdit(int id, CustomersMaintenance customer)
        {
            var redirect = RequireLogin();
            if (redirect != null)
            {
                return redirect;
            }

            if (id != customer.Uid)
            {
                return NotFound();
            }

            var entity = await _context.CustomersMaintenance.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            var oldSnapshot = CustomerAuditHelper.CreateMaintenanceSnapshot(entity);

            entity.CustomerNo = customer.CustomerNo;
            entity.BTNo = customer.BTNo;
            entity.CustomerName = customer.CustomerName;
            entity.CNICNo = customer.CNICNo;
            entity.FatherName = customer.FatherName;
            entity.MobileNo = customer.MobileNo;
            entity.TelephoneNo = customer.TelephoneNo;
            entity.Project = customer.Project;
            entity.SubProject = customer.SubProject;
            entity.TariffName = customer.TariffName;
            entity.Category = customer.Category;
            entity.Block = customer.Block;
            entity.Sector = customer.Sector;
            entity.PloNo = customer.PloNo;
            entity.PlotType = customer.PlotType;
            entity.Size = customer.Size;
            entity.City = customer.City;
            entity.MeterNo = customer.MeterNo;
            entity.BTNoMaintenance = customer.BTNoMaintenance;
            entity.BillStatusMaint = customer.BillStatusMaint;

            ModelState.Clear();
            if (!TryValidateModel(entity))
            {
                return View(entity);
            }

            var newSnapshot = CustomerAuditHelper.CreateMaintenanceSnapshot(entity);
            var (oldData, newData) = AuditDiffHelper.BuildDiff(oldSnapshot, newSnapshot);

            HttpContext.Items["SkipEfAudit"] = true;
            try
            {
                await _context.SaveChangesAsync();
            }
            finally
            {
                HttpContext.Items.Remove("SkipEfAudit");
            }

            if (oldData.Count > 0)
            {
                await _auditLogService.LogAsync(
                    CustomerAuditHelper.MCustomerTable,
                    "UPDATE",
                    CustomerAuditHelper.GetMaintenanceRecordId(entity),
                    oldData,
                    newData,
                    CustomerAuditHelper.MCustomerModule);
            }

            TempData["SuccessMessage"] = "Maintenance customer updated successfully!";
            return RedirectToAction(nameof(MCustomers));
        }

        private bool CustomersDetailExists(int id)
        {
            return _context.CustomersDetails.Any(e => e.Uid == id);
        }












        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Get all projects from Configurations table where ConfigKey = "project"
                var projects = await _context.Configurations
                    .Where(c => c.ConfigKey == "project")
                    .Select(c => c.ConfigValue)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToListAsync();

                // Get blocks from Configurations table for specific keys
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

                // Remove duplicates and sort
                var blocks = allBlocks.Distinct().OrderBy(b => b).ToList();

                // Get total customers count for all projects
                var totalCustomers = await _context.CustomersDetails.CountAsync();

                // Get customers count by project
                var projectStats = await _context.CustomersDetails
                    .Where(c => !string.IsNullOrEmpty(c.Project))
                    .GroupBy(c => c.Project)
                    .Select(g => new ProjectStatisticsViewModel
                    {
                        ProjectName = g.Key,
                        TotalCustomers = g.Count()
                    })
                    .OrderBy(p => p.ProjectName)
                    .ToListAsync();

                // Get customers count by block
                var blockStats = await _context.CustomersDetails
                    .Where(c => !string.IsNullOrEmpty(c.Block))
                    .GroupBy(c => c.Block)
                    .Select(g => new BlockStatisticsViewModel
                    {
                        BlockName = g.Key,
                        TotalCustomers = g.Count()
                    })
                    .OrderBy(b => b.BlockName)
                    .ToListAsync();

                // Prepare data for view
                ViewBag.Projects = projects ?? new List<string>();
                ViewBag.Blocks = blocks ?? new List<string>();
                ViewBag.MohlanwalBlocks = mohlanwalBlocks ?? new List<string>();
                ViewBag.OrchardsBlocks = orchardsBlocks ?? new List<string>();
                ViewBag.TotalAllCustomers = totalCustomers;
                ViewBag.ProjectStatistics = projectStats ?? new List<ProjectStatisticsViewModel>();
                ViewBag.BlockStatistics = blockStats ?? new List<BlockStatisticsViewModel>();

                return View();
            }
            catch (Exception ex)
            {
                // Log error if needed
                ViewBag.ErrorMessage = "Error loading dashboard data: " + ex.Message;
                return View();
            }
        }

        // Update the GetAllBlocksStatistics method
        // AJAX action to get all blocks statistics
        [HttpGet]
        public async Task<IActionResult> GetAllBlocksStatistics()
        {
            try
            {
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

                var statistics = new List<object>();

                foreach (var block in blocks)
                {
                    var totalCustomers = await _context.CustomersDetails
                        .Where(c => c.Block == block)
                        .CountAsync();

                    // Get projects distribution for this block
                    var projectsInBlock = await _context.CustomersDetails
                        .Where(c => c.Block == block)
                        .GroupBy(c => c.Project)
                        .Select(g => new
                        {
                            ProjectName = g.Key,
                            Count = g.Count()
                        })
                        .OrderByDescending(p => p.Count)
                        .Take(3) // Top 3 projects
                        .ToListAsync();

                    // Determine which configuration the block belongs to
                    var blockType = "Unknown";
                    if (mohlanwalBlocks != null && mohlanwalBlocks.Contains(block))
                        blockType = "Mohlanwal";
                    else if (orchardsBlocks != null && orchardsBlocks.Contains(block))
                        blockType = "Orchards";

                    statistics.Add(new
                    {
                        BlockName = block,
                        BlockType = blockType,
                        TotalCustomers = totalCustomers,
                        TopProjects = projectsInBlock
                    });
                }

                return Json(new
                {
                    success = true,
                    statistics = statistics
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






        // Controllers/SSQCustomersController.cs (add these methods)

        // GET: SSQCustomers/CustomersSelection
        public async Task<IActionResult> CustomersSelection()
        {
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

                // Get categories from Configurations table where ConfigKey = "Category" or "Categories"
                var plotTypes = await _context.Configurations
      .Where(c => c.ConfigKey == "PlotType") // Key change here
      .Select(c => c.ConfigValue)
      .Distinct()
      .OrderBy(p => p)
      .ToListAsync();

                ViewBag.Projects = projects ?? new List<string>();
                ViewBag.Blocks = blocks ?? new List<string>();
                ViewBag.Categories = plotTypes ?? new List<string>(); // Renamed for clarity, but keep 'Categories' 
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

        // POST: SSQCustomers/
        // 

        
        [HttpPost]
        public async Task<IActionResult> GetCustomersBySelection(string project, string block, string category)
        {
            try
            {
                var query = _context.CustomersDetails.AsQueryable();

                // Apply filters...
                if (!string.IsNullOrEmpty(project) && project != "All")
                    query = query.Where(c => c.Project == project);

                if (!string.IsNullOrEmpty(block) && block != "All")
                    query = query.Where(c => c.Block == block);

                if (!string.IsNullOrEmpty(category) && category != "All")
                    query = query.Where(c => c.PlotType == category); // Updated to PlotType

                var totalRecords = await query.CountAsync();

                // NO LONGER FETCHING CUSTOMER DETAILS
                return Json(new
                {
                    success = true,
                    totalRecords = totalRecords,  // Only returning count
                    message = $"Found {totalRecords} customer(s) matching the criteria"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: SSQCustomers/GetAllCustomersBySelection
        [HttpPost]
        public async Task<IActionResult> GetAllCustomersBySelection(string project, string block, string category)
        {
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