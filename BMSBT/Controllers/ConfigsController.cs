using Microsoft.AspNetCore.Mvc;

using BMSBT.Models;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;
namespace BMSBT.Controllers
{
    public class ConfigsController : Controller
    {
        public readonly BmsbtContext _context;
        public ConfigsController(BmsbtContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult AllConfigs(int? page, string key = null)
        {
            int pageSize = 10;
            int pageNumber = page ?? 1;

            var query = _context.Configurations.AsQueryable();

            if (!string.IsNullOrEmpty(key))
            {
                query = query.Where(c => c.ConfigKey == key);
            }

            var configs = query.OrderBy(c => c.ConfigId).ToPagedList(pageNumber, pageSize);

            return View(configs);
        }









        public IActionResult Create()
        {
            return View();
        }


        // Get the city-related ConfigValues from Configuration
        public IActionResult GetProjects()
        {
            var projects = _context.Configurations
                                 .Where(c => c.ConfigKey == "Lahore")
                                 .Select(c => c.ConfigValue)
                                 .ToList();
            return Json(projects); // Return JSON for dropdown
        }


        public IActionResult GetSectors(string project) 
        {
            var sectors = _context.Configurations
                                  .Where(c => c.ConfigKey == "Sector" + project)
                                  .Select(c => c.ConfigValue)
                                  .ToList();
            return Json(sectors); // Return JSON for dropdown
        }


        // Get the city-related ConfigValues from Configuration
        public IActionResult GetKeyValues()
        {
            var projects = _context.Configurations
                                 .Where(c => c.ConfigKey == "Key")
                                 .Select(c => c.ConfigValue)
                                 .ToList();
            return Json(projects); // Return JSON for dropdown
        }



        // Get the city-related ConfigValues from Configuration
        public IActionResult GetTariffs()
        {
            var tariffs = _context.Tarrifs.Select(t => new { t.Uid, t.TarrifName }).ToList();

            if (tariffs == null || !tariffs.Any())
            {
                Console.WriteLine("No tariffs found in the database.");
            }
            else
            {
                Console.WriteLine("Tariffs found: " + tariffs.Count);
            }

            return Json(tariffs); // Return JSON for dropdown
        }



        // Get the city-related ConfigValues from Configuration
        public IActionResult GetBillStatus()
        {
            var billstatus = _context.Configurations
                                 .Where(c => c.ConfigKey == "BillStatus")
                                 .Select(c => c.ConfigValue)
                                 .ToList();
            return Json(billstatus); // Return JSON for dropdown
        }

        // Get the city-related ConfigValues from Configuration
        public IActionResult GetSubProjects(string project)
        {
            var projects = _context.Configurations
                                 .Where(c => c.ConfigKey == project)
                                 .Select(c => c.ConfigValue)
                                 .ToList();
            return Json(projects); // Return JSON for dropdown
        }



        public IActionResult ConfigKeyValues(string project)
        {
            if (project == "All")
            {
                var db = _context.Configurations.ToList();
                return PartialView("_KeyValueGridView", db);
            }



            var customers = _context.Configurations
                                    .Where(c => c.ConfigKey == project)
                                    .ToList();
            return PartialView("_KeyValueGridView", customers);
        }



        // POST: Employee/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ConfigId,ConfigKey, ConfigValue")] Configuration configuration)
        {
            if (ModelState.IsValid)
            {
                _context.Add(configuration);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(AllConfigs));
            }
            return View(configuration);
        }


        // GET: Employee/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var configs = await _context.Configurations.FindAsync(id);
            if (configs == null)
            {
                return NotFound();
            }
            return View(configs);
        }



        // POST: Employee/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Uid, ConfigId,ConfigKey,ConfigValue")] Configuration configuration)
        {
            if (id != configuration.Uid)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(configuration);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ConfigurationExists(configuration.Uid))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(AllConfigs));
            }
            return View(configuration);
        }

        private bool ConfigurationExists(int id)
        {
            return _context.Configurations.Any(e => e.Uid == id);
        }




        // GET: Employee/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var config = await _context.Configurations.FirstOrDefaultAsync(m => m.Uid == id);
            if (config == null)
            {
                return NotFound();
            }

            return View(config);
        }




        // GET: Employee/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var config = await _context.Configurations.FirstOrDefaultAsync(m => m.Uid == id);
            if (config == null)
            {
                return NotFound();
            }

            return View(config);
        }

        // POST: Employee/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var config = await _context.Configurations.FindAsync(id);
            if (config == null)
            {
                // Optionally, return a custom error view or message
                return NotFound();
            }
            _context.Configurations.Remove(config);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(AllConfigs));
        }

        private bool EmployeeExists(int id)
        {
            return _context.Configurations.Any(e => e.Uid == id);
        }










    }
}