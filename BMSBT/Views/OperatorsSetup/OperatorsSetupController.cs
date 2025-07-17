using BMSBT.BillServices;
using BMSBT.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static DevExpress.CodeParser.CodeStyle.Formatting.Rules.Spacing;
using static DevExpress.XtraPrinting.Native.PageSizeInfo;
using X.PagedList;
using X.PagedList.EntityFramework;
using X.PagedList.Extensions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BMSBT.Controllers
{
    public class OperatorsSetupController : Controller
    {

        private readonly BmsbtContext _context;

        public OperatorsSetupController(BmsbtContext context)
        {
            _context = context;
        }


        // Display grid
        public IActionResult Index()
        {
            var list = _context.OperatorsSetups.ToList();
            return View(list);
        }

        // GET Create
        public IActionResult Create()
        {
            return View();
        }

        // POST Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(OperatorsSetup model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedBy = HttpContext.Session.GetString("UserName") ?? "System";
                model.CreatedOn = DateTime.Now;

                _context.OperatorsSetups.Add(model);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
        }




        //        // GET: OperatorsSetup
        //        public async Task<IActionResult> Index()
        //        {
        //            return View(await _context.OperatorsSetups.ToListAsync());
        //        }




        //        public async Task<IActionResult> AllOperators(string search, string sortOrder, int? page)
        //        {
        //            int pageSize = 10;
        //            int pageNumber = page ?? 1;

        //            var operatorsQuery = _context.OperatorsSetups.AsQueryable();

        //            // Search functionality
        //            if (!string.IsNullOrEmpty(search))
        //            {
        //                operatorsQuery = operatorsQuery.Where(o =>
        //                    o.OperatorId.Contains(search) ||
        //                    o.OperatorName.Contains(search) ||
        //                    o.BankName.Contains(search) ||
        //                    o.BillingMonth.Contains(search) ||
        //                    o.BillingYear.Contains(search));
        //            }

        //            // Sorting
        //            ViewData["OperatorIdSort"] = string.IsNullOrEmpty(sortOrder) ? "id_desc" : "";
        //            ViewData["OperatorNameSort"] = sortOrder == "name_asc" ? "name_desc" : "name_asc";

        //            operatorsQuery = sortOrder switch
        //            {
        //                "id_desc" => operatorsQuery.OrderByDescending(o => o.OperatorId),
        //                "name_asc" => operatorsQuery.OrderBy(o => o.OperatorName),
        //                "name_desc" => operatorsQuery.OrderByDescending(o => o.OperatorName),
        //                _ => operatorsQuery.OrderBy(o => o.OperatorId),
        //            };

        //            // **Fix: Materialize the query first with `ToListAsync()`**
        //            var operatorsList = await operatorsQuery.ToListAsync(); // Fetch the data first
        //            var pagedList = operatorsList.ToPagedList(pageNumber, pageSize); // Apply paging in-memory

        //            return View(pagedList);
        //        }







        //        [HttpGet("OperatorsSetup/Edit/{id}")]
        //        public async Task<IActionResult> Edit(string id)
        //        {
        //            if (string.IsNullOrEmpty(id))
        //            {
        //                return BadRequest();
        //            }

        //            var operatorData = await _context.OperatorsSetups.FirstOrDefaultAsync(o => o.OperatorId == id);
        //            if (operatorData == null)
        //            {
        //                return NotFound();
        //            }
        //            return View(operatorData);
        //        }







        //        [HttpPost]
        //        public async Task<IActionResult> Edit(OperatorsSetup model)
        //        {

        //            var existingOperator = await _context.OperatorsSetups.FirstOrDefaultAsync(o => o.OperatorId == model.OperatorId);
        //            if (existingOperator != null)
        //            {
        //                existingOperator.OperatorId = model.OperatorId;
        //                existingOperator.OperatorName = model.OperatorName;
        //                existingOperator.BankName = model.BankName;
        //                existingOperator.BillingMonth = model.BillingMonth;
        //                existingOperator.BillingYear = model.BillingYear;
        //                existingOperator.DueDate = model.DueDate;
        //                existingOperator.ReadingDate = model.ReadingDate;
        //                existingOperator.ValidDate = model.ValidDate;
        //                existingOperator.IssueDate = model.IssueDate;
        //                existingOperator.PaidDate = model.PaidDate;
        //                existingOperator.FPAYEAR1 = model.FPAYEAR1;
        //                existingOperator.FPARate1=model.FPARate1;
        //                existingOperator.FPAMonth1 = model.FPAMonth1;
        //                await _context.SaveChangesAsync();
        //                return RedirectToAction("Logout", "Login");

        //            }
        //            else
        //            {
        //                return NotFound();
        //            }

        //        }



        //        // GET: Details
        //        public IActionResult Details(string id)
        //        {
        //            var operatorSetup = _context.OperatorsSetups.FirstOrDefault(o => o.OperatorId == id);
        //            if (operatorSetup == null)
        //            {
        //                return NotFound();
        //            }
        //            return View(operatorSetup);
        //        }


        //        [HttpGet]
        //        public IActionResult Create()
        //        {
        //            var operatorId = HttpContext.Session.GetString("OperatorId");
        //            var operatorname = HttpContext.Session.GetString("UserName");

        //            ViewBag.BankList = new SelectList(new List<SelectListItem>
        //{
        //    new SelectListItem { Text = "HBL", Value = "HBL" },
        //    new SelectListItem { Text = "UBL", Value = "UBL" },
        //    new SelectListItem { Text = "MCB", Value = "MCB" },
        //}, "Value", "Text");



        //            var model = new OperatorsSetup
        //            {
        //                OperatorId = operatorId,
        //                OperatorName = operatorname
        //            };

        //            return View(model);
        //        }


        //        [HttpPost]
        //        [ValidateAntiForgeryToken]
        //        public IActionResult Create(OperatorsSetup model)
        //        {
        //            if (ModelState.IsValid)
        //            {
        //                model.CreatedBy = HttpContext.Session.GetString("UserName");
        //                model.CreatedOn = DateTime.Now;

        //                _context.OperatorsSetups.Add(model);
        //                _context.SaveChanges();

        //                return RedirectToAction("AllOperators");
        //            }

        //            return View(model);
        //        }


        //        private bool OperatorsSetupExists(string OperatorId)
        //        {
        //            return _context.OperatorsSetups.Any(e => e.OperatorId == OperatorId);
        //        }
    }
}