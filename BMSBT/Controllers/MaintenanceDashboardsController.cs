using BMSBT.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BMSBT.Controllers
{
    /// <summary>
    /// Maintenance module dashboards (Customers, Bills, Collections, Users) with bar-chart summaries.
    /// </summary>
    public class MaintenanceDashboardsController : Controller
    {
        private readonly BmsbtContext _dbContext;

        private static readonly string[] MonthNames =
        {
            "January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December"
        };

        public MaintenanceDashboardsController(BmsbtContext context)
        {
            _dbContext = context;
        }

        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");
            base.OnActionExecuting(context);
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Customers));
        }

        public IActionResult Customers()
        {
            ViewBag.ActiveDashboard = "Customers";

            var byProject = _dbContext.CustomersMaintenance
                .AsNoTracking()
                .Where(c => !string.IsNullOrWhiteSpace(c.Project))
                .GroupBy(c => c.Project.Trim())
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderBy(x => x.Label)
                .ToList();

            ViewBag.Chart1Labels = byProject.Select(x => x.Label).ToList();
            ViewBag.Chart1Values = byProject.Select(x => (double)x.Count).ToList();

            var byCategory = _dbContext.CustomersMaintenance
                .AsNoTracking()
                .GroupBy(c => c.Category)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(15)
                .ToList();

            ViewBag.Chart2Labels = byCategory.Select(x => x.Label ?? "").ToList();
            ViewBag.Chart2Values = byCategory.Select(x => (double)x.Count).ToList();

            return View();
        }

        public IActionResult Bills(string? billingYear, string? billingMonth)
        {
            ViewBag.ActiveDashboard = "Bills";

            var yearOptions = GetBillingYearOptions();
            var year = string.IsNullOrWhiteSpace(billingYear)
                ? DateTime.Now.Year.ToString()
                : billingYear.Trim();
            if (!yearOptions.Contains(year))
            {
                yearOptions.Insert(0, year);
                yearOptions = yearOptions.Distinct().OrderByDescending(y => int.TryParse(y, out var n) ? n : 0).ToList();
            }

            var monthNormalized = NormalizeBillingMonth(billingMonth);

            var billsYear = _dbContext.MaintenanceBills
                .AsNoTracking()
                .Where(b => b.BillingYear == year)
                .ToList();

            ViewBag.Chart1Labels = MonthNames.ToList();
            ViewBag.Chart1Values = MonthNames
                .Select(m => (double)billsYear.Count(b => b.BillingMonth == m))
                .ToList();

            var billsForStatus = string.IsNullOrEmpty(monthNormalized)
                ? billsYear
                : billsYear.Where(b => MonthEquals(b.BillingMonth, monthNormalized)).ToList();

            int paid = billsForStatus.Count(b => IsPaidStatus(b.PaymentStatus));
            int surcharge = billsForStatus.Count(b => IsSurchargeStatus(b.PaymentStatus));
            int partial = billsForStatus.Count(b => IsPartialStatus(b.PaymentStatus));
            int unpaid = billsForStatus.Count(b => IsUnpaidStatus(b.PaymentStatus));

            ViewBag.Chart2Labels = new List<string> { "Paid", "Paid w/ surcharge", "Partially paid", "Unpaid" };
            ViewBag.Chart2Values = new List<double> { paid, surcharge, partial, unpaid };

            ViewBag.BillsYear = year;
            ViewBag.BillingYearOptions = yearOptions;
            ViewBag.MonthNameOptions = MonthNames.ToList();
            ViewBag.SelectedBillingYear = year;
            ViewBag.SelectedBillingMonth = monthNormalized ?? "";
            ViewBag.BillsStatusScopeLabel = string.IsNullOrEmpty(monthNormalized)
                ? $"all months in {year}"
                : $"{monthNormalized} {year}";
            return View();
        }

        public IActionResult Collections(string? billingYear, string? billingMonth)
        {
            ViewBag.ActiveDashboard = "Collections";

            var yearOptions = GetBillingYearOptions();
            var year = string.IsNullOrWhiteSpace(billingYear)
                ? DateTime.Now.Year.ToString()
                : billingYear.Trim();
            if (!yearOptions.Contains(year))
            {
                yearOptions.Insert(0, year);
                yearOptions = yearOptions.Distinct().OrderByDescending(y => int.TryParse(y, out var n) ? n : 0).ToList();
            }

            var monthNormalized = NormalizeBillingMonth(billingMonth);

            var billsYear = _dbContext.MaintenanceBills
                .AsNoTracking()
                .Where(b => b.BillingYear == year)
                .ToList();

            var collectedByMonth = MonthNames.Select(m =>
            {
                var inMonth = billsYear.Where(b => b.BillingMonth == m && HasCollectedPayment(b)).ToList();
                var sum = inMonth.Sum(b =>
                {
                    var amt = b.PaymentAmount ?? b.BillAmountInDueDate ?? 0;
                    return (double)amt;
                });
                return sum;
            }).ToList();

            ViewBag.Chart1Labels = MonthNames.ToList();
            ViewBag.Chart1Values = collectedByMonth;

            var billsForTotals = string.IsNullOrEmpty(monthNormalized)
                ? billsYear
                : billsYear.Where(b => MonthEquals(b.BillingMonth, monthNormalized)).ToList();

            var totalOutstanding = billsForTotals
                .Where(b => IsUnpaidStatus(b.PaymentStatus))
                .Sum(b => (double)(b.BillAmountInDueDate ?? 0));

            var totalCollected = billsForTotals
                .Where(b => HasCollectedPayment(b))
                .Sum(b => (double)(b.PaymentAmount ?? b.BillAmountInDueDate ?? 0));

            ViewBag.Chart2Labels = new List<string> { "Collected (paid)", "Outstanding (unpaid)" };
            ViewBag.Chart2Values = new List<double> { totalCollected, totalOutstanding };

            ViewBag.Chart1DatasetLabel = "Amount (PKR)";
            ViewBag.Chart2DatasetLabel = "Amount (PKR)";

            ViewBag.CollectionsYear = year;
            ViewBag.BillingYearOptions = yearOptions;
            ViewBag.MonthNameOptions = MonthNames.ToList();
            ViewBag.SelectedBillingYear = year;
            ViewBag.SelectedBillingMonth = monthNormalized ?? "";
            ViewBag.CollectionsTotalsScopeLabel = string.IsNullOrEmpty(monthNormalized)
                ? $"all months in {year}"
                : $"{monthNormalized} {year}";
            return View();
        }

        public IActionResult Users()
        {
            ViewBag.ActiveDashboard = "Users";

            var roleGroups = _dbContext.Users
                .AsNoTracking()
                .GroupBy(u => string.IsNullOrWhiteSpace(u.Role) ? "(No role)" : u.Role!.Trim())
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            ViewBag.Chart1Labels = roleGroups.Select(x => x.Role).ToList();
            ViewBag.Chart1Values = roleGroups.Select(x => (double)x.Count).ToList();

            var totalUsers = _dbContext.Users.AsNoTracking().Count();
            var withRole = _dbContext.Users.AsNoTracking().Count(u => !string.IsNullOrWhiteSpace(u.Role));
            var withoutRole = totalUsers - withRole;

            ViewBag.Chart2Labels = new List<string> { "With role assigned", "Without role" };
            ViewBag.Chart2Values = new List<double> { withRole, withoutRole };

            return View();
        }

        private static bool HasCollectedPayment(MaintenanceBill b)
        {
            return IsPaidStatus(b.PaymentStatus)
                   || IsSurchargeStatus(b.PaymentStatus)
                   || IsPartialStatus(b.PaymentStatus);
        }

        private static bool IsPaidStatus(string? s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            var t = s.Trim();
            return string.Equals(t, "paid", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSurchargeStatus(string? s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            var t = s.Trim();
            return t.Equals("paid with surcharge", StringComparison.OrdinalIgnoreCase)
                   || t.Equals("Paid with Surcharge", StringComparison.OrdinalIgnoreCase)
                   || t.Equals("PaidWithSurcharge", StringComparison.OrdinalIgnoreCase)
                   || t.Equals("Paid with surcharge", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPartialStatus(string? s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            var t = s.Trim();
            return t.Equals("paritally paid", StringComparison.OrdinalIgnoreCase)
                   || t.Equals("Partially Paid", StringComparison.OrdinalIgnoreCase)
                   || t.Equals("partially paid", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsUnpaidStatus(string? s)
        {
            if (string.IsNullOrEmpty(s)) return true;
            var t = s.Trim();
            return t.Equals("unpaid", StringComparison.OrdinalIgnoreCase)
                   || t.Equals("Unpaid", StringComparison.OrdinalIgnoreCase);
        }

        private List<string> GetBillingYearOptions()
        {
            var raw = _dbContext.MaintenanceBills
                .AsNoTracking()
                .Where(b => !string.IsNullOrWhiteSpace(b.BillingYear))
                .Select(b => b.BillingYear!.Trim())
                .Distinct()
                .ToList();

            var ordered = raw
                .Select(y => new { Y = y, N = int.TryParse(y, out var n) ? n : int.MinValue })
                .Where(x => x.N != int.MinValue)
                .OrderByDescending(x => x.N)
                .Select(x => x.Y)
                .ToList();

            var cy = DateTime.Now.Year.ToString();
            if (!ordered.Contains(cy))
                ordered.Insert(0, cy);

            return ordered.Count > 0 ? ordered : new List<string> { cy };
        }

        /// <summary>Returns null if "all months"; otherwise a canonical month name from <see cref="MonthNames"/>.</summary>
        private static string? NormalizeBillingMonth(string? billingMonth)
        {
            if (string.IsNullOrWhiteSpace(billingMonth))
                return null;

            var t = billingMonth.Trim();
            var match = MonthNames.FirstOrDefault(m =>
                string.Equals(m, t, StringComparison.OrdinalIgnoreCase));
            return match;
        }

        private static bool MonthEquals(string? dbMonth, string canonicalMonth)
        {
            if (string.IsNullOrWhiteSpace(dbMonth)) return false;
            return string.Equals(dbMonth.Trim(), canonicalMonth, StringComparison.OrdinalIgnoreCase);
        }
    }
}
