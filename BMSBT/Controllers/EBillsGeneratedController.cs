using BMSBT.BillServices;
using BMSBT.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using X.PagedList.Extensions;

namespace BMSBT.Controllers
{
    public class EBillsGeneratedController : Controller
    {
        private readonly BmsbtContext _dbContext;
        public EBillsGeneratedController(BmsbtContext dbContext)
        {
            _dbContext = dbContext;            
        }



        public IActionResult Index()
        {
            return View();
        }




        public IActionResult Generated(string project, string sector, string block, int? page)
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



    }
}
