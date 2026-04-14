using BMSBT.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BMSBT.Controllers
{
    public class ActivitiesController : Controller
    {
        private readonly BmsbtContext _context;

        public ActivitiesController(BmsbtContext context)
        {
            _context = context;
        }

        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");
            base.OnActionExecuting(context);
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> HistoryLogs(DateTime? fromDate, DateTime? toDate, string? operation, string? btno, bool applyFilter = false)
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedOperation = operation;
            ViewBag.Btno = btno;
            ViewBag.ApplyFilter = applyFilter;

            ViewBag.Operations = await _context.AuditLogs
                .AsNoTracking()
                .Select(l => l.Operation)
                .Where(o => !string.IsNullOrEmpty(o))
                .Distinct()
                .OrderBy(o => o)
                .ToListAsync();

            if (!applyFilter)
            {
                return View(new List<AuditLog>());
            }

            var hasCriteria = fromDate.HasValue || toDate.HasValue || !string.IsNullOrWhiteSpace(operation) || !string.IsNullOrWhiteSpace(btno);
            if (!hasCriteria)
            {
                return View(new List<AuditLog>());
            }

            var query = _context.AuditLogs
                .AsNoTracking()
                .OrderByDescending(l => l.ChangedAt)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(l => l.ChangedAt >= from);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(l => l.ChangedAt <= to);
            }

            if (!string.IsNullOrWhiteSpace(operation))
            {
                query = query.Where(l => l.Operation == operation);
            }

            if (!string.IsNullOrWhiteSpace(btno))
            {
                query = query.Where(l => l.RecordId != null && l.RecordId.Contains(btno));
            }

            var logs = await query.Take(500).ToListAsync();
            return View(logs);
        }
    }
}
