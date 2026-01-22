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

        public IActionResult AI()
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            SetUserContext();
            return View();
        }

        public async Task<IActionResult> DataScience(string project = null, string year = null)
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            SetUserContext();

            // Get available projects and years for filters
            var projects = await _context.CustomersMaintenance
                .Select(c => c.Project)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();

            var yearsFromDb = await _context.MaintenanceBills
                .Where(b => !string.IsNullOrEmpty(b.BillingYear))
                .Select(b => b.BillingYear)
                .Distinct()
                .ToListAsync();

            // Add hardcoded years and combine with database years
            var hardcodedYears = new List<string> { "2026", "2025", "2024" };
            var allYears = hardcodedYears.Union(yearsFromDb)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            ViewBag.Projects = new SelectList(projects, project);
            ViewBag.Years = new SelectList(allYears, year);
            ViewBag.SelectedProject = project;
            ViewBag.SelectedYear = year;

            // Build query with filters
            var billsQuery = _context.MaintenanceBills.AsQueryable();
            var customersQuery = _context.CustomersMaintenance.AsQueryable();

            if (!string.IsNullOrEmpty(project))
            {
                customersQuery = customersQuery.Where(c => c.Project == project);
                billsQuery = billsQuery.Where(b => customersQuery.Any(c => c.BTNo == b.Btno));
            }

            if (!string.IsNullOrEmpty(year))
            {
                billsQuery = billsQuery.Where(b => b.BillingYear == year);
            }

            // 1. Overall Summary Statistics
            var totalCustomers = await customersQuery.CountAsync();
            var totalBills = await billsQuery.CountAsync();
            var totalBilledAmount = await billsQuery.SumAsync(b => b.BillAmountInDueDate ?? 0);
            var totalCollectedAmount = await billsQuery
                .Where(b => b.PaymentStatus != null && 
                           (b.PaymentStatus.ToLower() == "paid" || 
                            b.PaymentStatus.ToLower() == "paid with surcharge" ||
                            b.PaymentStatus.ToLower() == "paidwithsurcharge"))
                .SumAsync(b => b.BillAmountInDueDate ?? 0);
            var totalArrears = await billsQuery.SumAsync(b => b.Arrears ?? 0);
            var totalUnpaidBills = await billsQuery
                .Where(b => b.PaymentStatus == null || 
                           b.PaymentStatus.ToLower() == "unpaid" ||
                           b.PaymentStatus.ToLower() == "partially paid")
                .CountAsync();
            var totalUnpaidAmount = await billsQuery
                .Where(b => b.PaymentStatus == null || 
                           b.PaymentStatus.ToLower() == "unpaid" ||
                           b.PaymentStatus.ToLower() == "partially paid")
                .SumAsync(b => b.BillAmountAfterDueDate ?? 0);

            ViewBag.TotalCustomers = totalCustomers;
            ViewBag.TotalBills = totalBills;
            ViewBag.TotalBilledAmount = totalBilledAmount;
            ViewBag.TotalCollectedAmount = totalCollectedAmount;
            ViewBag.TotalArrears = totalArrears;
            ViewBag.TotalUnpaidBills = totalUnpaidBills;
            ViewBag.TotalUnpaidAmount = totalUnpaidAmount;
            ViewBag.CollectionRate = totalBilledAmount > 0 ? (totalCollectedAmount / totalBilledAmount * 100) : 0;
            ViewBag.AverageBillAmount = totalBills > 0 ? (totalBilledAmount / totalBills) : 0;

            // 2. Monthly Billing Summary
            var monthlySummary = await billsQuery
                .Where(b => !string.IsNullOrEmpty(b.BillingMonth) && !string.IsNullOrEmpty(b.BillingYear))
                .GroupBy(b => new { b.BillingMonth, b.BillingYear })
                .Select(g => new
                {
                    Month = g.Key.BillingMonth,
                    Year = g.Key.BillingYear,
                    BillCount = g.Count(),
                    TotalAmount = g.Sum(b => b.BillAmountInDueDate ?? 0),
                    CollectedAmount = g.Where(b => b.PaymentStatus != null && 
                                                   (b.PaymentStatus.ToLower() == "paid" || 
                                                    b.PaymentStatus.ToLower() == "paid with surcharge" ||
                                                    b.PaymentStatus.ToLower() == "paidwithsurcharge"))
                                      .Sum(b => b.BillAmountInDueDate ?? 0),
                    UnpaidCount = g.Count(b => b.PaymentStatus == null || 
                                              b.PaymentStatus.ToLower() == "unpaid" ||
                                              b.PaymentStatus.ToLower() == "partially paid")
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            ViewBag.MonthlySummary = monthlySummary;

            // 3. Payment Status Distribution
            var paymentStatusDistribution = await billsQuery
                .GroupBy(b => b.PaymentStatus ?? "Unpaid")
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count(),
                    Amount = g.Sum(b => b.BillAmountInDueDate ?? 0)
                })
                .ToListAsync();

            ViewBag.PaymentStatusDistribution = paymentStatusDistribution;

            // 4. Customer Segmentation by Arrears
            var customerArrears = await billsQuery
                .Where(b => (b.Arrears ?? 0) > 0)
                .GroupBy(b => b.Btno)
                .Select(g => new
                {
                    BTNo = g.Key,
                    TotalArrears = g.Sum(b => b.Arrears ?? 0),
                    BillCount = g.Count()
                })
                .OrderByDescending(x => x.TotalArrears)
                .Take(20)
                .ToListAsync();

            ViewBag.TopArrearsCustomers = customerArrears;

            // 5. Project-wise Analysis
            var projectAnalysis = await billsQuery
                .Where(b => !string.IsNullOrEmpty(b.Btno))
                .Join(_context.CustomersMaintenance.Where(c => !string.IsNullOrEmpty(c.BTNo)),
                    bill => bill.Btno,
                    customer => customer.BTNo,
                    (bill, customer) => new { bill, customer.Project })
                .GroupBy(x => x.Project)
                .Select(g => new
                {
                    Project = g.Key ?? "Unknown",
                    BillCount = g.Count(),
                    TotalAmount = g.Sum(x => x.bill.BillAmountInDueDate ?? 0),
                    CollectedAmount = g.Where(x => x.bill.PaymentStatus != null && 
                                                  (x.bill.PaymentStatus.ToLower() == "paid" || 
                                                   x.bill.PaymentStatus.ToLower() == "paid with surcharge" ||
                                                   x.bill.PaymentStatus.ToLower() == "paidwithsurcharge"))
                                      .Sum(x => x.bill.BillAmountInDueDate ?? 0),
                    TotalArrears = g.Sum(x => x.bill.Arrears ?? 0)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToListAsync();

            ViewBag.ProjectAnalysis = projectAnalysis;

            // 6. Yearly Trends
            var yearlyTrends = await billsQuery
                .Where(b => !string.IsNullOrEmpty(b.BillingYear))
                .GroupBy(b => b.BillingYear)
                .Select(g => new
                {
                    Year = g.Key,
                    BillCount = g.Count(),
                    TotalAmount = g.Sum(b => b.BillAmountInDueDate ?? 0),
                    CollectedAmount = g.Where(b => b.PaymentStatus != null && 
                                                 (b.PaymentStatus.ToLower() == "paid" || 
                                                  b.PaymentStatus.ToLower() == "paid with surcharge" ||
                                                  b.PaymentStatus.ToLower() == "paidwithsurcharge"))
                                      .Sum(b => b.BillAmountInDueDate ?? 0)
                })
                .OrderBy(x => x.Year)
                .ToListAsync();

            ViewBag.YearlyTrends = yearlyTrends;

            // 7. Overdue Accounts (Bills past due date)
            var today = DateOnly.FromDateTime(DateTime.Now);
            var overdueAccountsRaw = await billsQuery
                .Where(b => b.DueDate.HasValue && 
                           b.DueDate.Value < today &&
                           (b.PaymentStatus == null || 
                            b.PaymentStatus.ToLower() == "unpaid" ||
                            b.PaymentStatus.ToLower() == "partially paid"))
                .Select(b => new
                {
                    b.Btno,
                    b.CustomerName,
                    b.BillingMonth,
                    b.BillingYear,
                    b.DueDate,
                    b.BillAmountAfterDueDate
                })
                .ToListAsync();

            var overdueAccounts = overdueAccountsRaw
                .Select(b => new
                {
                    b.Btno,
                    b.CustomerName,
                    b.BillingMonth,
                    b.BillingYear,
                    b.DueDate,
                    b.BillAmountAfterDueDate,
                    DaysOverdue = b.DueDate.HasValue ? (DateTime.Now.Date - b.DueDate.Value.ToDateTime(TimeOnly.MinValue).Date).Days : 0
                })
                .OrderByDescending(b => b.DaysOverdue)
                .Take(20)
                .ToList();

            ViewBag.OverdueAccounts = overdueAccounts;

            return View();
        }
    }
}
