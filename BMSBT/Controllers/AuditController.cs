using BMSBT.Models;
using BMSBT.Roles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using X.PagedList;
using X.PagedList.Extensions;

namespace BMSBT.Controllers
{
    [CustomAuthorize("Admin,Audit,COO")]
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

        public IActionResult AuditLogs(string? tableName, string? operation, int? page)
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }

            SetUserContext();

            var query = _context.AuditLogs
                .AsNoTracking()
                .OrderByDescending(a => a.ChangedAt)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(tableName))
            {
                query = query.Where(a => a.TableName == tableName);
            }

            if (!string.IsNullOrWhiteSpace(operation))
            {
                query = query.Where(a => a.Operation == operation);
            }

            ViewBag.TableNames = _context.AuditLogs
                .AsNoTracking()
                .Select(a => a.TableName)
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct()
                .OrderBy(t => t)
                .ToList();
            ViewBag.SelectedTableName = tableName;

            ViewBag.Operations = _context.AuditLogs
                .AsNoTracking()
                .Select(a => a.Operation)
                .Where(o => !string.IsNullOrEmpty(o))
                .Distinct()
                .OrderBy(o => o)
                .ToList();
            ViewBag.SelectedOperation = operation;

            const int pageSize = 30;
            var pageNumber = page ?? 1;

            return View(query.ToPagedList(pageNumber, pageSize));
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
            ViewBag.CollectionRate = totalBilledAmount > 0 ? ((decimal)totalCollectedAmount / totalBilledAmount * 100) : 0;
            ViewBag.AverageBillAmount = totalBills > 0 ? ((decimal)totalBilledAmount / totalBills) : 0;

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

        [HttpGet]
        public IActionResult AuditSummary(string? billingMonth, string? billingYear, string? paymentStatus, int? page)
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }

            SetUserContext();

            ViewBag.BillingMonths = Enumerable.Range(1, 12).Select(i => new SelectListItem
            {
                Value = new DateTime(2000, i, 1).ToString("MMMM"),
                Text = new DateTime(2000, i, 1).ToString("MMMM")
            }).ToList();

            ViewBag.BillingYears = _context.ElectricityBills
                .AsNoTracking()
                .Where(b => !string.IsNullOrEmpty(b.BillingYear))
                .Select(b => b.BillingYear!)
                .Distinct()
                .OrderByDescending(y => y)
                .Select(y => new SelectListItem { Value = y, Text = y })
                .ToList();

            ViewBag.SelectedBillingMonth = billingMonth;
            ViewBag.SelectedBillingYear = billingYear;
            ViewBag.SelectedPaymentStatus = string.IsNullOrWhiteSpace(paymentStatus) ? "All" : paymentStatus.Trim();

            if (string.IsNullOrWhiteSpace(billingMonth) || string.IsNullOrWhiteSpace(billingYear))
            {
                return View(new List<ElectricityBill>().ToPagedList(1, 25));
            }

            var query = BuildAuditSummaryQuery(billingMonth, billingYear, paymentStatus);

            const int pageSize = 25;
            var pageNumber = page ?? 1;

            return View(query.ToPagedList(pageNumber, pageSize));
        }

        [HttpGet]
        public async Task<IActionResult> ExportAuditSummaryToExcel(string? billingMonth, string? billingYear, string? paymentStatus)
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }

            if (string.IsNullOrWhiteSpace(billingMonth) || string.IsNullOrWhiteSpace(billingYear))
            {
                return RedirectToAction(nameof(AuditSummary));
            }

            var bills = await BuildAuditSummaryQuery(billingMonth, billingYear, paymentStatus).ToListAsync();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Electricity Audit Summary");

            var headers = new[]
            {
                "BT No", "Billing Month", "Billing Year", "Bill Amount",
                "Amount In Due Date", "Payment Status", "Payment Date", "Payment Method"
            };

            for (int col = 0; col < headers.Length; col++)
            {
                worksheet.Cells[1, col + 1].Value = headers[col];
                worksheet.Cells[1, col + 1].Style.Font.Bold = true;
            }

            for (int i = 0; i < bills.Count; i++)
            {
                var bill = bills[i];
                var row = i + 2;
                worksheet.Cells[row, 1].Value = bill.Btno;
                worksheet.Cells[row, 2].Value = bill.BillingMonth;
                worksheet.Cells[row, 3].Value = bill.BillingYear;
                worksheet.Cells[row, 4].Value = bill.BillAmount;
                worksheet.Cells[row, 5].Value = bill.BillAmountInDueDate;
                worksheet.Cells[row, 6].Value = string.IsNullOrWhiteSpace(bill.PaymentStatus) ? "Unpaid" : bill.PaymentStatus;
                worksheet.Cells[row, 7].Value = bill.PaymentDate?.ToString("dd-MMM-yyyy") ?? "";
                worksheet.Cells[row, 8].Value = bill.PaymentMethod ?? "";
            }

            if (worksheet.Dimension != null)
            {
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            }

            var statusSuffix = string.IsNullOrWhiteSpace(paymentStatus) || paymentStatus == "All"
                ? "All"
                : paymentStatus.Trim();
            var fileName = $"ElectricityAuditSummary_{billingMonth}_{billingYear}_{statusSuffix}.xlsx";

            return File(
                package.GetAsByteArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        private IQueryable<ElectricityBill> BuildAuditSummaryQuery(string billingMonth, string billingYear, string? paymentStatus)
        {
            var query = _context.ElectricityBills
                .AsNoTracking()
                .Where(b => b.BillingMonth == billingMonth && b.BillingYear == billingYear);

            var statusFilter = string.IsNullOrWhiteSpace(paymentStatus) ? "All" : paymentStatus.Trim();
            if (statusFilter == "Paid")
            {
                query = query.Where(b => b.PaymentStatus != null && b.PaymentStatus.ToLower() == "paid");
            }
            else if (statusFilter == "Unpaid")
            {
                query = query.Where(b =>
                    b.PaymentStatus == null
                    || b.PaymentStatus == ""
                    || b.PaymentStatus.ToLower() == "unpaid");
            }

            return query
                .OrderBy(b => b.Btno)
                .ThenBy(b => b.Uid);
        }

        [HttpGet]
        public IActionResult MaintenanceAuditSummary(string? billingMonth, string? billingYear, string? paymentStatus, int? page)
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }

            SetUserContext();

            ViewBag.BillingMonths = Enumerable.Range(1, 12).Select(i => new SelectListItem
            {
                Value = new DateTime(2000, i, 1).ToString("MMMM"),
                Text = new DateTime(2000, i, 1).ToString("MMMM")
            }).ToList();

            ViewBag.BillingYears = _context.MaintenanceBills
                .AsNoTracking()
                .Where(b => !string.IsNullOrEmpty(b.BillingYear))
                .Select(b => b.BillingYear!)
                .Distinct()
                .OrderByDescending(y => y)
                .Select(y => new SelectListItem { Value = y, Text = y })
                .ToList();

            ViewBag.SelectedBillingMonth = billingMonth;
            ViewBag.SelectedBillingYear = billingYear;
            ViewBag.SelectedPaymentStatus = string.IsNullOrWhiteSpace(paymentStatus) ? "All" : paymentStatus.Trim();

            if (string.IsNullOrWhiteSpace(billingMonth) || string.IsNullOrWhiteSpace(billingYear))
            {
                return View(new List<MaintenanceBill>().ToPagedList(1, 25));
            }

            var query = BuildMaintenanceAuditSummaryQuery(billingMonth, billingYear, paymentStatus);

            const int pageSize = 25;
            var pageNumber = page ?? 1;

            return View(query.ToPagedList(pageNumber, pageSize));
        }

        [HttpGet]
        public async Task<IActionResult> ExportMaintenanceAuditSummaryToExcel(string? billingMonth, string? billingYear, string? paymentStatus)
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }

            if (string.IsNullOrWhiteSpace(billingMonth) || string.IsNullOrWhiteSpace(billingYear))
            {
                return RedirectToAction(nameof(MaintenanceAuditSummary));
            }

            var bills = await BuildMaintenanceAuditSummaryQuery(billingMonth, billingYear, paymentStatus).ToListAsync();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Maintenance Audit Summary");

            var headers = new[]
            {
                "BT No", "Billing Month", "Billing Year", "Maint Charges",
                "Amount In Due Date", "Payment Status", "Payment Date", "Payment Method"
            };

            for (int col = 0; col < headers.Length; col++)
            {
                worksheet.Cells[1, col + 1].Value = headers[col];
                worksheet.Cells[1, col + 1].Style.Font.Bold = true;
            }

            for (int i = 0; i < bills.Count; i++)
            {
                var bill = bills[i];
                var row = i + 2;
                worksheet.Cells[row, 1].Value = bill.Btno;
                worksheet.Cells[row, 2].Value = bill.BillingMonth;
                worksheet.Cells[row, 3].Value = bill.BillingYear;
                worksheet.Cells[row, 4].Value = bill.MaintCharges;
                worksheet.Cells[row, 5].Value = bill.BillAmountInDueDate;
                worksheet.Cells[row, 6].Value = string.IsNullOrWhiteSpace(bill.PaymentStatus) ? "Unpaid" : bill.PaymentStatus;
                worksheet.Cells[row, 7].Value = bill.PaymentDate?.ToString("dd-MMM-yyyy") ?? "";
                worksheet.Cells[row, 8].Value = bill.PaymentMethod ?? "";
            }

            if (worksheet.Dimension != null)
            {
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            }

            var statusSuffix = string.IsNullOrWhiteSpace(paymentStatus) || paymentStatus == "All"
                ? "All"
                : paymentStatus.Trim();
            var fileName = $"MaintenanceAuditSummary_{billingMonth}_{billingYear}_{statusSuffix}.xlsx";

            return File(
                package.GetAsByteArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        private IQueryable<MaintenanceBill> BuildMaintenanceAuditSummaryQuery(string billingMonth, string billingYear, string? paymentStatus)
        {
            var query = _context.MaintenanceBills
                .AsNoTracking()
                .Where(b => b.BillingMonth == billingMonth && b.BillingYear == billingYear);

            var statusFilter = string.IsNullOrWhiteSpace(paymentStatus) ? "All" : paymentStatus.Trim();
            if (statusFilter == "Paid")
            {
                query = query.Where(b =>
                    b.PaymentStatus != null
                    && (b.PaymentStatus.ToLower() == "paid"
                        || b.PaymentStatus.ToLower() == "paid with surcharge"
                        || b.PaymentStatus.ToLower() == "paidwithsurcharge"));
            }
            else if (statusFilter == "Unpaid")
            {
                query = query.Where(b =>
                    b.PaymentStatus == null
                    || b.PaymentStatus == ""
                    || b.PaymentStatus.ToLower() == "unpaid"
                    || b.PaymentStatus.ToLower() == "partially paid"
                    || b.PaymentStatus.ToLower() == "paritally paid");
            }

            return query
                .OrderBy(b => b.Btno)
                .ThenBy(b => b.Uid);
        }
    }
}
