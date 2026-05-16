using BMSBT.Models;
using BMSBT.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BMSBT.Controllers;

/// <summary>
/// Maintenance billing reports hub (Summary, Collections, Recovery, Customers, Billing, Users).
/// </summary>
public class MaintenanceBillingReportsController : Controller
{
    private readonly BmsbtContext _dbContext;

    private static readonly string[] MonthNames =
    {
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    };

    public MaintenanceBillingReportsController(BmsbtContext context)
    {
        _dbContext = context;
    }

    public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
    {
        ViewBag.UserName = HttpContext.Session.GetString("UserName");
        ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");
        base.OnActionExecuting(context);
    }

    public IActionResult Index() => RedirectToAction(nameof(Summary));

    public IActionResult Summary(string? billingYear, string? billingMonth, bool load = false)
    {
        ViewBag.ActiveReport = "Summary";
        return View(ResolveReport(billingYear, billingMonth, load, null));
    }

    public IActionResult Collections(string? billingYear, string? billingMonth, bool load = false)
    {
        ViewBag.ActiveReport = "Collections";
        return View(ResolveReport(billingYear, billingMonth, load, PopulateCollectionCharts));
    }

    public IActionResult Recovery(string? billingYear, string? billingMonth, bool load = false)
    {
        ViewBag.ActiveReport = "Recovery";
        return View(ResolveReport(billingYear, billingMonth, load, PopulateRecoveryCharts));
    }

    public IActionResult Customers(string? billingYear, string? billingMonth, bool load = false)
    {
        ViewBag.ActiveReport = "Customers";
        return View(ResolveReport(billingYear, billingMonth, load, (vm, _, _) => PopulateCustomerCharts(vm)));
    }

    public IActionResult Billing(string? billingYear, string? billingMonth, bool load = false)
    {
        ViewBag.ActiveReport = "Billing";
        return View(ResolveReport(billingYear, billingMonth, load, PopulateBillingCharts));
    }

    public IActionResult Users(string? billingYear, string? billingMonth, bool load = false)
    {
        ViewBag.ActiveReport = "Users";
        return View(ResolveReport(billingYear, billingMonth, load, (vm, _, _) => PopulateUserCharts(vm)));
    }

    private MaintenanceBillingReportViewModel ResolveReport(
        string? billingYear,
        string? billingMonth,
        bool load,
        Action<MaintenanceBillingReportViewModel, string?, string?>? populateCharts)
    {
        if (!load)
            return CreateFilterShell(billingYear, billingMonth);

        var vm = BuildReportContext(billingYear, billingMonth);
        vm.HasResults = true;
        populateCharts?.Invoke(vm, billingYear, billingMonth);
        return vm;
    }

    private MaintenanceBillingReportViewModel CreateFilterShell(string? billingYear, string? billingMonth)
    {
        var yearOptions = GetBillingYearOptions();
        var year = string.IsNullOrWhiteSpace(billingYear)
            ? DateTime.Now.Year.ToString()
            : billingYear.Trim();
        if (!yearOptions.Contains(year))
        {
            yearOptions.Insert(0, year);
            yearOptions = yearOptions.Distinct()
                .OrderByDescending(y => int.TryParse(y, out var n) ? n : 0).ToList();
        }

        var monthNormalized = NormalizeBillingMonth(billingMonth);

        return new MaintenanceBillingReportViewModel
        {
            HasResults = false,
            BillingYear = year,
            BillingMonth = monthNormalized,
            ScopeLabel = "Select filters and click Load Report",
            YearOptions = yearOptions,
            MonthOptions = MonthNames.ToList()
        };
    }

    private MaintenanceBillingReportViewModel BuildReportContext(string? billingYear, string? billingMonth)
    {
        var yearOptions = GetBillingYearOptions();
        var year = string.IsNullOrWhiteSpace(billingYear)
            ? DateTime.Now.Year.ToString()
            : billingYear.Trim();
        if (!yearOptions.Contains(year))
        {
            yearOptions.Insert(0, year);
            yearOptions = yearOptions.Distinct()
                .OrderByDescending(y => int.TryParse(y, out var n) ? n : 0).ToList();
        }

        var monthNormalized = NormalizeBillingMonth(billingMonth);

        var billsYear = _dbContext.MaintenanceBills
            .AsNoTracking()
            .Where(b => b.BillingYear == year)
            .ToList();

        var billsScoped = string.IsNullOrEmpty(monthNormalized)
            ? billsYear
            : billsYear.Where(b => MonthEquals(b.BillingMonth, monthNormalized)).ToList();

        var totalBilled = billsScoped.Sum(b => (double)(b.BillAmountInDueDate ?? 0));
        var totalCollected = billsScoped
            .Where(HasCollectedPayment)
            .Sum(b => (double)(b.PaymentAmount ?? b.BillAmountInDueDate ?? 0));
        var totalOutstanding = billsScoped
            .Where(b => IsUnpaidStatus(b.PaymentStatus))
            .Sum(b => (double)(b.BillAmountInDueDate ?? 0));

        var recovery = totalBilled > 0
            ? Math.Round(totalCollected / totalBilled * 100, 1)
            : 0;

        return new MaintenanceBillingReportViewModel
        {
            HasResults = true,
            BillingYear = year,
            BillingMonth = monthNormalized,
            ScopeLabel = string.IsNullOrEmpty(monthNormalized)
                ? $"All months in {year}"
                : $"{monthNormalized} {year}",
            YearOptions = yearOptions,
            MonthOptions = MonthNames.ToList(),
            TotalCustomers = _dbContext.CustomersMaintenance.AsNoTracking().Count(),
            TotalBills = billsScoped.Count,
            TotalBilledAmount = totalBilled,
            TotalCollected = totalCollected,
            TotalOutstanding = totalOutstanding,
            RecoveryPercent = recovery,
            TotalUsers = _dbContext.Users.AsNoTracking().Count(),
            PaidBillsCount = billsScoped.Count(b => HasCollectedPayment(b)),
            UnpaidBillsCount = billsScoped.Count(b => IsUnpaidStatus(b.PaymentStatus))
        };
    }

    private void PopulateCollectionCharts(MaintenanceBillingReportViewModel vm, string? billingYear, string? billingMonth)
    {
        var year = vm.BillingYear;
        var billsYear = _dbContext.MaintenanceBills.AsNoTracking()
            .Where(b => b.BillingYear == year).ToList();

        ViewBag.Chart1Labels = MonthNames.ToList();
        ViewBag.Chart1Values = MonthNames.Select(m =>
            billsYear.Where(b => b.BillingMonth == m && HasCollectedPayment(b))
                .Sum(b => (double)(b.PaymentAmount ?? b.BillAmountInDueDate ?? 0))).ToList();
        ViewBag.Chart1DatasetLabel = "Amount (PKR)";

        ViewBag.Chart2Labels = new List<string> { "Collected", "Outstanding" };
        ViewBag.Chart2Values = new List<double> { vm.TotalCollected, vm.TotalOutstanding };
        ViewBag.Chart2DatasetLabel = "Amount (PKR)";
    }

    private void PopulateRecoveryCharts(MaintenanceBillingReportViewModel vm, string? billingYear, string? billingMonth)
    {
        var year = vm.BillingYear;
        var billsYear = _dbContext.MaintenanceBills.AsNoTracking()
            .Where(b => b.BillingYear == year).ToList();

        ViewBag.Chart1Labels = MonthNames.ToList();
        ViewBag.Chart1Values = MonthNames.Select(m =>
        {
            var inMonth = billsYear.Where(b => b.BillingMonth == m).ToList();
            var billed = inMonth.Sum(b => (double)(b.BillAmountInDueDate ?? 0));
            var collected = inMonth.Where(HasCollectedPayment)
                .Sum(b => (double)(b.PaymentAmount ?? b.BillAmountInDueDate ?? 0));
            return billed > 0 ? Math.Round(collected / billed * 100, 1) : 0;
        }).ToList();
        ViewBag.Chart1DatasetLabel = "Recovery %";

        ViewBag.Chart2Labels = new List<string> { "Recovered amount", "Outstanding amount" };
        ViewBag.Chart2Values = new List<double> { vm.TotalCollected, vm.TotalOutstanding };
        ViewBag.Chart2DatasetLabel = "Amount (PKR)";
    }

    private void PopulateCustomerCharts(MaintenanceBillingReportViewModel vm)
    {
        var byProject = _dbContext.CustomersMaintenance.AsNoTracking()
            .Where(c => !string.IsNullOrWhiteSpace(c.Project))
            .GroupBy(c => c.Project.Trim())
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .OrderBy(x => x.Label)
            .Take(12)
            .ToList();

        ViewBag.Chart1Labels = byProject.Select(x => x.Label).ToList();
        ViewBag.Chart1Values = byProject.Select(x => (double)x.Count).ToList();
        ViewBag.Chart1DatasetLabel = "Customers";

        var byBlock = _dbContext.CustomersMaintenance.AsNoTracking()
            .GroupBy(c => c.Block)
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(12)
            .ToList();

        ViewBag.Chart2Labels = byBlock.Select(x => x.Label ?? "").ToList();
        ViewBag.Chart2Values = byBlock.Select(x => (double)x.Count).ToList();
        ViewBag.Chart2DatasetLabel = "Customers";
    }

    private void PopulateBillingCharts(MaintenanceBillingReportViewModel vm, string? billingYear, string? billingMonth)
    {
        var year = vm.BillingYear;
        var monthNormalized = vm.BillingMonth;
        var billsYear = _dbContext.MaintenanceBills.AsNoTracking()
            .Where(b => b.BillingYear == year).ToList();

        ViewBag.Chart1Labels = MonthNames.ToList();
        ViewBag.Chart1Values = MonthNames
            .Select(m => (double)billsYear.Count(b => b.BillingMonth == m)).ToList();
        ViewBag.Chart1DatasetLabel = "Bill count";

        var billsScoped = string.IsNullOrEmpty(monthNormalized)
            ? billsYear
            : billsYear.Where(b => MonthEquals(b.BillingMonth, monthNormalized)).ToList();

        ViewBag.Chart2Labels = new List<string>
            { "Paid", "Paid w/ surcharge", "Partially paid", "Unpaid" };
        ViewBag.Chart2Values = new List<double>
        {
            billsScoped.Count(b => IsPaidStatus(b.PaymentStatus)),
            billsScoped.Count(b => IsSurchargeStatus(b.PaymentStatus)),
            billsScoped.Count(b => IsPartialStatus(b.PaymentStatus)),
            billsScoped.Count(b => IsUnpaidStatus(b.PaymentStatus))
        };
        ViewBag.Chart2DatasetLabel = "Bill count";
    }

    private void PopulateUserCharts(MaintenanceBillingReportViewModel vm)
    {
        var roleGroups = _dbContext.Users.AsNoTracking()
            .GroupBy(u => string.IsNullOrWhiteSpace(u.Role) ? "(No role)" : u.Role!.Trim())
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        ViewBag.Chart1Labels = roleGroups.Select(x => x.Role).ToList();
        ViewBag.Chart1Values = roleGroups.Select(x => (double)x.Count).ToList();
        ViewBag.Chart1DatasetLabel = "Users";

        var withRole = _dbContext.Users.AsNoTracking().Count(u => !string.IsNullOrWhiteSpace(u.Role));
        var withoutRole = vm.TotalUsers - withRole;

        ViewBag.Chart2Labels = new List<string> { "With role", "Without role" };
        ViewBag.Chart2Values = new List<double> { withRole, withoutRole };
        ViewBag.Chart2DatasetLabel = "Users";
    }

    private static bool HasCollectedPayment(MaintenanceBill b) =>
        IsPaidStatus(b.PaymentStatus) || IsSurchargeStatus(b.PaymentStatus) || IsPartialStatus(b.PaymentStatus);

    private static bool IsPaidStatus(string? s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        return string.Equals(s.Trim(), "paid", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSurchargeStatus(string? s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        var t = s.Trim();
        return t.Equals("paid with surcharge", StringComparison.OrdinalIgnoreCase)
               || t.Equals("Paid with Surcharge", StringComparison.OrdinalIgnoreCase)
               || t.Equals("PaidWithSurcharge", StringComparison.OrdinalIgnoreCase);
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
        var raw = _dbContext.MaintenanceBills.AsNoTracking()
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

    private static string? NormalizeBillingMonth(string? billingMonth)
    {
        if (string.IsNullOrWhiteSpace(billingMonth)) return null;
        return MonthNames.FirstOrDefault(m =>
            string.Equals(m, billingMonth.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static bool MonthEquals(string? dbMonth, string canonicalMonth)
    {
        if (string.IsNullOrWhiteSpace(dbMonth)) return false;
        return string.Equals(dbMonth.Trim(), canonicalMonth, StringComparison.OrdinalIgnoreCase);
    }
}
