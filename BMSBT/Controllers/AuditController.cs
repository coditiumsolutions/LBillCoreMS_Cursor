using BMSBT.Models;
using BMSBT.Roles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.Extensions;

namespace BMSBT.Controllers
{
    public class AuditController : Controller
    {
        private readonly BmsbtContext _context;

        public AuditController(BmsbtContext context)
        {
            _context = context;
        }

        private void SetUserContext()
        {
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            SetUserContext();
            return View();
        }

        public async Task<IActionResult> OperatorsLog(string changedBy, int? page)
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            SetUserContext();

            // Populate Operators dropdown
            var operators = await _context.OperatorsSetups
                .Select(o => o.OperatorName)
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct()
                .OrderBy(n => n)
                .ToListAsync();

            ViewBag.Operators = new SelectList(operators, changedBy);
            ViewBag.SelectedChangedBy = changedBy;

            // Query AuditLogs for "Operators Setup" module
            var query = _context.AuditLogs
                .Where(l => l.ModuleName == "Operators Setup")
                .OrderByDescending(l => l.ChangedAt)
                .AsQueryable();

            if (!string.IsNullOrEmpty(changedBy))
            {
                query = query.Where(l => l.ChangedBy == changedBy);
            }

            int pageSize = 20;
            int pageNumber = page ?? 1;

            var pagedData = query.ToPagedList(pageNumber, pageSize);

            return View(pagedData);
        }
    }
}
