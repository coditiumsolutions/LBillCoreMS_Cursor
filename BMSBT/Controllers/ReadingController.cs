using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using BMSBT.Models;
using System.Globalization;
using static DevExpress.XtraPrinting.Native.PageSizeInfo;
using BMSBT.Roles;
using BMSBT.Services;
using BMSBT.ViewModels;
using X.PagedList;
using X.PagedList.Extensions;

namespace BMSBT.Controllers
{
    public class ReadingController : Controller
    {
        private readonly BmsbtContext _context; // Your DbContext
        private const int AddReadingPageSize = 50;

        public ReadingController(BmsbtContext context)
        {
            _context = context;
        }


        public IActionResult Index(string search)
        {
            return View(BuildLastThreeMonthsReadingStats());
        }


        public IActionResult Dashboard()
        {
            return View(BuildLastThreeMonthsReadingStats());
        }

        private List<MonthlyReadingStat> BuildLastThreeMonthsReadingStats()
        {
            var userName = HttpContext.Session.GetString("UserName");
            var operatorId = HttpContext.Session.GetString("OperatorId");
            var operatorSetup = OperatorSetupResolver.Resolve(_context, userName, operatorId);

            var anchorMonth = operatorSetup?.BillingMonth?.Trim();
            var anchorYear = operatorSetup?.BillingYear?.Trim();

            var periods = GetLastThreeBillingPeriods(anchorMonth, anchorYear);

            var totalCustomers = _context.CustomersDetails
                .Count(c => c.Btno != null && c.Btno.Trim() != "");

            var monthNames = periods.Select(p => p.Month).Distinct().ToList();
            var years = periods.Select(p => p.Year).Distinct().ToList();

            var readings = _context.ReadingSheets
                .Where(r => r.Month != null && r.Year != null
                            && monthNames.Contains(r.Month)
                            && years.Contains(r.Year))
                .Select(r => new { r.Btno, Month = r.Month!, Year = r.Year! })
                .ToList()
                .Where(r => periods.Any(p =>
                    string.Equals(p.Month, r.Month.Trim(), StringComparison.OrdinalIgnoreCase)
                    && string.Equals(p.Year, r.Year.Trim(), StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var stats = new List<MonthlyReadingStat>();
            foreach (var period in periods)
            {
                var monthRows = readings
                    .Where(r => string.Equals(r.Month.Trim(), period.Month, StringComparison.OrdinalIgnoreCase)
                                && string.Equals(r.Year.Trim(), period.Year, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var entered = monthRows.Count;
                var customersWithReading = monthRows
                    .Where(r => !string.IsNullOrWhiteSpace(r.Btno))
                    .Select(r => r.Btno!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();

                stats.Add(new MonthlyReadingStat
                {
                    Month = period.Month,
                    Year = period.Year,
                    TotalEntered = entered,
                    Pending = Math.Max(0, totalCustomers - customersWithReading)
                });
            }

            ViewBag.AnchorMonth = periods.Count > 0 ? periods[^1].Month : null;
            ViewBag.AnchorYear = periods.Count > 0 ? periods[^1].Year : null;
            ViewBag.OperatorName = operatorSetup?.OperatorName ?? userName;

            return stats;
        }

        /// <summary>
        /// Returns three periods ending at the operator billing month (or calendar month), oldest first.
        /// </summary>
        private static List<(string Month, string Year)> GetLastThreeBillingPeriods(string? monthName, string? yearText)
        {
            var monthMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["January"] = 1, ["February"] = 2, ["March"] = 3, ["April"] = 4,
                ["May"] = 5, ["June"] = 6, ["July"] = 7, ["August"] = 8,
                ["September"] = 9, ["October"] = 10, ["November"] = 11, ["December"] = 12
            };

            int month;
            int year;
            if (!string.IsNullOrWhiteSpace(monthName)
                && monthMap.TryGetValue(monthName.Trim(), out month)
                && int.TryParse(yearText, out year))
            {
                // use operator period
            }
            else
            {
                month = DateTime.Now.Month;
                year = DateTime.Now.Year;
            }

            var result = new List<(string Month, string Year)>();
            var cursor = new DateTime(year, month, 1);
            for (int i = 2; i >= 0; i--)
            {
                var d = cursor.AddMonths(-i);
                result.Add((d.ToString("MMMM", CultureInfo.InvariantCulture), d.Year.ToString()));
            }

            return result;
        }


        public IActionResult ViewReading(int id)
        {
            var reading = _context.ReadingSheets.FirstOrDefault(r => r.Uid == id); // use your PK field
            if (reading == null)
                return NotFound();

            return View(reading); // Create View: Views/ReadingSheet/ViewReading.cshtml
        }



        [HttpPost]
        public IActionResult UpdateReading(int id, int previousReading, int presentReading, string billingMonth)
        {
            var reading = _context.ReadingSheets.FirstOrDefault(r => r.Uid == id);
            if (reading == null)
                return NotFound();

            string updatedBy = HttpContext.Session.GetString("UserName") ?? "Unknown";
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            string historyEntry = $"[{timestamp}] Previous: {reading.Previous1} → {previousReading}, " +
                                  $"Present: {reading.Present1} → {presentReading}, " +
                                  $"Month: {reading.Month} → {billingMonth} by {updatedBy}";

            // Apply changes
            reading.Previous1 = previousReading;
            reading.Present1 = presentReading;
            reading.Month = billingMonth;


            // ✅ Set CreatedBy and CreatedOn if not already set (optional)
            if (string.IsNullOrEmpty(reading.CreatedBy))
                reading.CreatedBy = updatedBy;

            //if (reading.CreatedOn == null || reading.CreatedOn.Value == DateTime.MinValue)
                reading.CreatedOn = DateTime.Now;


            // Append history
            reading.History = string.IsNullOrEmpty(reading.History)
                ? historyEntry
                : reading.History + Environment.NewLine + historyEntry;

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Reading updated successfully.";
            return RedirectToAction("ViewReading", new { id = id });
        }






        public IActionResult Search(string search)
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.Username = HttpContext.Session.GetString("UserName");
            ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");


            // If search is empty, return an empty list
            if (string.IsNullOrEmpty(search))
            {
                return View(new List<ReadingSheet>());
            }

            var readingSheets = _context.ReadingSheets
                .Where(r => (r.Btno != null && r.Btno.Contains(search)) ||
                            (r.CustomerNo != null && r.CustomerNo.Contains(search)))
                .ToList();

            return View(readingSheets);

        }


        public IActionResult UploadExcel()
        {
            return View();
        }

        [HttpPost]
        public IActionResult UploadExcelFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Message"] = "Please select a valid Excel file!";
                return RedirectToAction("UploadExcel");
            }

            using (var stream = new MemoryStream())
            {
                file.CopyTo(stream);
                using (var package = new ExcelPackage(stream))
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension.Rows;

                    List<ReadingSheet> readingList = new List<ReadingSheet>();
                    List<string> duplicateRecords = new List<string>();

                    // Get the username from session
                    string? username = HttpContext.Session.GetString("UserName") ?? "UnknownUser";
                    string uploadInfo = $"Uploaded By: {username} - {DateTime.Now:yyyy-MM-dd HH:mm:ss}";



                    // Get the current date and time
                    string currentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);



                    for (int row = 2; row <= rowCount; row++) // Start from row 2 to skip headers
                    {
                        string? btno = worksheet.Cells[row, 1].Value?.ToString();
                        string? month = worksheet.Cells[row, 2].Value?.ToString();
                        string? year = worksheet.Cells[row, 3].Value?.ToString();
                        int? present1 = worksheet.Cells[row, 4].Value != null ? Convert.ToInt32(worksheet.Cells[row, 8].Value) : (int?)null;

                        if (!string.IsNullOrEmpty(btno) && !string.IsNullOrEmpty(year) &&
                                !string.IsNullOrEmpty(month) && present1.HasValue)
                        {


                            // Check if record already exists in the database
                            bool exists = _context.ReadingSheets.Any(r => r.Btno == btno && r.Year == year && r.Month == month);
                            if (!exists)
                            {

                                readingList.Add(new ReadingSheet
                                {
                                    Btno = worksheet.Cells[row, 1].Text,
                                    Month = worksheet.Cells[row, 2].Text,
                                    Year = worksheet.Cells[row, 3].Text,
                                    Previous1 = int.TryParse(worksheet.Cells[row, 4].Text, out int p1) ? p1 : (int?)null,
                                    Present1 = int.TryParse(worksheet.Cells[row, 5].Text, out int pr1) ? pr1 : (int?)null,



                                    //Previous1 = int.TryParse(worksheet.Cells[row, 12].Text, out int p1) ? p1 : (int?)null,
                                    //Present1 = int.TryParse(worksheet.Cells[row, 4].Text, out int pr1) ? pr1 : (int?)null,

                                    //Previous2 = 0, // int.TryParse(worksheet.Cells[row, 15].Text, out int p2) ? p2 : (int?)null,
                                    //Present2 = 0, //int.TryParse(worksheet.Cells[row, 5].Text, out int pr2) ? pr2 : (int?)null,

                                    //Previous3 = 0, //  int.TryParse(worksheet.Cells[row, 17].Text, out int p3) ? p3 : (int?)null,
                                    //Present3 = 0, // int.TryParse(worksheet.Cells[row, 6].Text, out int pr3) ? pr3 : (int?)null,


                                    CustomerNo = uploadInfo
                                    //TarrifName = worksheet.Cells[row, 5].Text,
                                    // MeterType = worksheet.Cells[row, 6].Text,
                                    // Previous1 = int.TryParse(worksheet.Cells[row, 7].Text, out int p1) ? p1 : (int?)null,
                                    //Present1 = int.TryParse(worksheet.Cells[row, 8].Text, out int pr1) ? pr1 : (int?)null,
                                    //Difference1 = int.TryParse(worksheet.Cells[row, 9].Text, out int d1) ? d1 : (int?)null,
                                    //Previous2 = int.TryParse(worksheet.Cells[row, 10].Text, out int p2) ? p2 : (int?)null,
                                    //Present2 = int.TryParse(worksheet.Cells[row, 11].Text, out int pr2) ? pr2 : (int?)null,
                                    //Difference2 = int.TryParse(worksheet.Cells[row, 12].Text, out int d2) ? d2 : (int?)null,
                                    //Previous3 = int.TryParse(worksheet.Cells[row, 13].Text, out int p3) ? p3 : (int?)null,
                                    //Present3 = int.TryParse(worksheet.Cells[row, 14].Text, out int pr3) ? pr3 : (int?)null,
                                    //Difference3 = int.TryParse(worksheet.Cells[row, 15].Text, out int d3) ? d3 : (int?)null
                                });
                            }

                            else
                            {
                                // Append duplicate info
                                duplicateRecords.Add($"Btno: {btno}, Month: {month}, Year: {year}");
                            }

                        }
                    }

                    // Save only new records
                    if (readingList.Count > 0)
                    {
                        _context.ReadingSheets.AddRange(readingList);
                        _context.SaveChanges();
                    }
                    // Message to show user
                    string message = $"Data uploaded successfully! {readingList.Count} new records added.";
                    if (duplicateRecords.Count > 0)
                    {
                        message += $" {duplicateRecords.Count} records were not uploaded because they already exist.";
                    }
                    TempData["Message"] = message;

                    //_context.ReadingSheets.AddRange(readingList);
                    //_context.SaveChanges();
                }
            }


            //TempData["Message"] = "Data uploaded successfully!";
            return RedirectToAction("UploadExcel");
        }




        public IActionResult ShowReading(string? search, string? billingMonth, string? billingYear, int page = 1)
        {
            int pageSize = 10; // Show 10 records per page

            // Populate dropdown lists
            ViewBag.BillingMonths = _context.ReadingSheets
                                             .Select(r => r.Month)
                                             .Distinct()
                                             .OrderBy(m => m)
                                             .ToList();

            ViewBag.BillingYears = _context.ReadingSheets
                                            .Select(r => r.Year)
                                            .Distinct()
                                            .OrderBy(y => y)
                                            .ToList();

            var readings = _context.ReadingSheets.AsQueryable();

            // Store filter values in ViewBag to persist them in the pagination links
            ViewBag.Search = search;
            ViewBag.BillingMonth = billingMonth;
            ViewBag.BillingYear = billingYear;

            // If no filter is selected, return an empty list
            if (string.IsNullOrEmpty(billingMonth) && string.IsNullOrEmpty(billingYear) && string.IsNullOrEmpty(search))
            {
                ViewBag.CurrentPage = 1;
                ViewBag.TotalPages = 1;
                return View(new List<ReadingSheet>()); // Empty list on first load
            }

            // Apply filters
            if (!string.IsNullOrEmpty(billingMonth))
            {
                readings = readings.Where(r => r.Month == billingMonth);
            }

            if (!string.IsNullOrEmpty(billingYear))
            {
                readings = readings.Where(r => r.Year == billingYear);
            }

            if (!string.IsNullOrEmpty(search))
            {
                readings = readings.Where(r => r.Btno.Contains(search) || r.CustomerNo.Contains(search));
            }

            // Get total count AFTER filtering
            int totalRecords = readings.Count();
            ViewBag.TotalRecords = totalRecords; // Store total number of bills in ViewBag

            // If no records found, return empty list
            if (totalRecords == 0)
            {
                ViewBag.CurrentPage = 1;
                ViewBag.TotalPages = 1;
                return View(new List<ReadingSheet>());
            }

            // Apply sorting before pagination (ensures stable ordering)
            var paginatedReadings = readings
                                     .OrderBy(r => r.Uid) // Adjust sorting based on your model
                                     .Skip((page - 1) * pageSize)
                                     .Take(pageSize)
                                     .ToList();

            // Pass pagination details to View
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            return View(paginatedReadings);
        }






        [HttpPost]
        public IActionResult DeleteSelected(List<int> selectedReadings)
        {
            if (selectedReadings == null || selectedReadings.Count == 0)
            {
                TempData["Message"] = "No records selected for deletion!";
                return RedirectToAction("ShowReading");
            }

            var readingsToDelete = _context.ReadingSheets
                .Where(r => selectedReadings.Contains(r.Uid))
                .ToList();

            if (readingsToDelete.Any())
            {
                _context.ReadingSheets.RemoveRange(readingsToDelete);
                _context.SaveChanges();
                TempData["Message"] = $"{readingsToDelete.Count} records deleted successfully!";
            }
            else
            {
                TempData["Message"] = "No matching records found!";
            }

            return RedirectToAction("ShowReading");
        }







        // GET: Edit Reading
        public IActionResult EditReading(int id)
        {
            var reading = _context.ReadingSheets.FirstOrDefault(r => r.Uid == id);
            if (reading == null)
            {
                return NotFound();
            }

            // Populate dropdown lists
            ViewBag.BillingMonths = new List<string>
    {
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    };

            ViewBag.BillingYears = Enumerable.Range(DateTime.Now.Year - 5, 6)
                                             .Select(y => y.ToString())
                                             .ToList();

            return View(reading);
        }

        // POST: Save Edited Reading
        [HttpPost]
        public IActionResult EditReading(ReadingSheet model)
        {
            if (ModelState.IsValid)
            {
                var existingReading = _context.ReadingSheets.FirstOrDefault(r => r.Uid == model.Uid);
                if (existingReading == null)
                {
                    return NotFound();
                }

                // Prevent duplicates (except for the same record)
                var duplicate = _context.ReadingSheets.Any(r => r.Btno == model.Btno
                                                                 && r.Month == model.Month
                                                                 && r.Year == model.Year
                                                                 && r.Uid != model.Uid);
                if (duplicate)
                {
                    ModelState.AddModelError("", "This reading already exists for the selected month and year.");
                    return View(model);
                }

                // Update the reading record
                existingReading.Btno = model.Btno;
                existingReading.Month = model.Month;
                existingReading.Year = model.Year;
                existingReading.CustomerNo = model.CustomerNo;
                existingReading.Present1 = model.Present1;

                _context.SaveChanges();
                TempData["SuccessMessage"] = "Reading updated successfully!";
                return RedirectToAction("ShowReading");
            }

            return View(model);
        }



        // GET: Create Reading
        public IActionResult CreateReading(string btno = null)
        {
            PopulateReadingDropdowns();

            var model = new ReadingSheet();

            // Default month/year from operator setup when available
            string? userName = HttpContext.Session.GetString("UserName");
            if (!string.IsNullOrWhiteSpace(userName))
            {
                var operatorSetup = _context.OperatorsSetups
                    .AsEnumerable()
                    .FirstOrDefault(o => string.Equals(o.OperatorName?.Trim(), userName.Trim(), StringComparison.OrdinalIgnoreCase)
                                      || string.Equals(o.OperatorID?.Trim(), userName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (operatorSetup != null)
                {
                    model.Month = operatorSetup.BillingMonth ?? model.Month;
                    model.Year = operatorSetup.BillingYear ?? model.Year;
                }
            }

            if (!string.IsNullOrEmpty(btno))
            {
                var customer = _context.CustomersDetails.FirstOrDefault(c => c.Btno == btno);
                if (customer != null)
                {
                    model.Btno = customer.Btno!;
                    model.CustomerNo = customer.CustomerNo;
                    model.TarrifName = customer.TariffName;
                    model.MeterType = customer.MeterType;

                    // Prefill previous reading from last saved present reading for this BTNo
                    var lastReading = _context.ReadingSheets
                        .Where(r => r.Btno == customer.Btno)
                        .OrderByDescending(r => r.Uid)
                        .FirstOrDefault();
                    if (lastReading?.Present1 != null)
                    {
                        model.Previous1 = lastReading.Present1;
                    }
                }
            }

            return View(model);
        }

        // POST: Create Reading
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateReading(ReadingSheet model)
        {
            PopulateReadingDropdowns();

            if (string.IsNullOrWhiteSpace(model.Btno) ||
                string.IsNullOrWhiteSpace(model.Month) ||
                string.IsNullOrWhiteSpace(model.Year))
            {
                ModelState.AddModelError("", "BT No, Month and Year are required.");
                return View(model);
            }

            model.Btno = model.Btno.Trim();
            model.Month = model.Month.Trim();
            model.Year = model.Year.Trim();

            // Unique: BTNo + Month + Year
            bool exists = _context.ReadingSheets.Any(r =>
                r.Btno == model.Btno &&
                r.Month == model.Month &&
                r.Year == model.Year);

            if (exists)
            {
                ModelState.AddModelError("", $"A reading already exists for BT No {model.Btno} in {model.Month} {model.Year}.");
                return View(model);
            }

            if (model.Previous1 == null || model.Present1 == null)
            {
                ModelState.AddModelError("", "Previous Reading and Current Reading are required.");
                return View(model);
            }

            if (model.Previous1 < 0 || model.Present1 < 0)
            {
                ModelState.AddModelError("", "Readings cannot be negative.");
                return View(model);
            }

            if (model.Previous1 > model.Present1)
            {
                ModelState.AddModelError("", "Previous Reading cannot be greater than Current Reading.");
                return View(model);
            }

            // Fill customer fields if missing
            var customer = _context.CustomersDetails.FirstOrDefault(c => c.Btno == model.Btno);
            if (customer != null)
            {
                model.CustomerNo ??= customer.CustomerNo;
                model.TarrifName ??= customer.TariffName;
                model.MeterType ??= customer.MeterType;
            }

            model.Difference1 = model.Present1 - model.Previous1;
            model.CreatedOn = DateTime.Now;
            model.CreatedBy = HttpContext.Session.GetString("UserName") ?? "System";
            model.History = $"Created on {model.CreatedOn:yyyy-MM-dd HH:mm} by {model.CreatedBy}";

            _context.ReadingSheets.Add(model);
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Reading saved for BT No {model.Btno} ({model.Month} {model.Year}).";
            return RedirectToAction(nameof(AddReading));
        }

        private void PopulateReadingDropdowns()
        {
            ViewBag.BillingMonths = new List<string>
            {
                "January", "February", "March", "April", "May", "June",
                "July", "August", "September", "October", "November", "December"
            };

            ViewBag.BillingYears = Enumerable.Range(DateTime.Now.Year - 5, 8)
                .Select(y => y.ToString())
                .ToList();
        }

        /// <summary>
        /// Customer list for selecting a customer and opening the Add Reading form.
        /// </summary>
        public IActionResult AddReading(string selectedProject, string selectedBlock, string btNoSearch, int? page)
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }

            string? userName = HttpContext.Session.GetString("UserName");
            ViewBag.UserName = userName;
            ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");

            string? billingMonth = null;
            string? billingYear = null;
            if (!string.IsNullOrWhiteSpace(userName))
            {
                var operatorSetup = _context.OperatorsSetups
                    .AsEnumerable()
                    .FirstOrDefault(o => string.Equals(o.OperatorName?.Trim(), userName.Trim(), StringComparison.OrdinalIgnoreCase)
                                      || string.Equals(o.OperatorID?.Trim(), userName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (operatorSetup != null)
                {
                    billingMonth = operatorSetup.BillingMonth?.Trim();
                    billingYear = operatorSetup.BillingYear?.Trim();
                }
            }

            ViewBag.BillingMonth = billingMonth;
            ViewBag.BillingYear = billingYear;

            var projects = _context.CustomersDetails
                .Where(p => p.Project != null && p.Project.Trim() != "")
                .Select(p => p.Project!.Trim())
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            var blocks = new List<string>();
            if (!string.IsNullOrWhiteSpace(selectedProject))
            {
                var trimProject = selectedProject.Trim();
                blocks = _context.CustomersDetails
                    .Where(c => c.Project != null
                                && c.Project.Trim() == trimProject
                                && c.Block != null
                                && c.Block.Trim() != "")
                    .Select(c => c.Block!.Trim())
                    .Distinct()
                    .OrderBy(b => b)
                    .ToList();
            }

            IPagedList<AddReadingCustomerRow> customers =
                new StaticPagedList<AddReadingCustomerRow>(Array.Empty<AddReadingCustomerRow>(), 1, AddReadingPageSize, 0);

            if (!string.IsNullOrWhiteSpace(selectedProject))
            {
                var trimProject = selectedProject.Trim();
                var query = _context.CustomersDetails
                    .Where(c => c.Project != null && c.Project.Trim() == trimProject);

                if (!string.IsNullOrWhiteSpace(selectedBlock))
                {
                    var trimBlock = selectedBlock.Trim();
                    query = query.Where(c => c.Block != null && c.Block.Trim() == trimBlock);
                }

                if (!string.IsNullOrWhiteSpace(btNoSearch))
                {
                    var term = btNoSearch.Trim();
                    query = query.Where(c =>
                        (c.Btno != null && c.Btno.Contains(term)) ||
                        (c.CustomerName != null && c.CustomerName.Contains(term)) ||
                        (c.PloNo != null && c.PloNo.Contains(term)));
                }

                var pageNumber = page ?? 1;
                var pagedCustomers = query
                    .OrderBy(c => c.Block)
                    .ThenBy(c => c.Btno)
                    .ToPagedList(pageNumber, AddReadingPageSize);

                var pageBtNos = pagedCustomers
                    .Where(c => !string.IsNullOrWhiteSpace(c.Btno))
                    .Select(c => c.Btno!)
                    .Distinct()
                    .ToList();

                Dictionary<string, ReadingSheet> readingsByBtno = new(StringComparer.OrdinalIgnoreCase);
                if (pageBtNos.Count > 0
                    && !string.IsNullOrWhiteSpace(billingMonth)
                    && !string.IsNullOrWhiteSpace(billingYear))
                {
                    readingsByBtno = _context.ReadingSheets
                        .Where(r => r.Month == billingMonth
                                    && r.Year == billingYear
                                    && r.Btno != null
                                    && pageBtNos.Contains(r.Btno))
                        .AsEnumerable()
                        .GroupBy(r => r.Btno!, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Uid).First(), StringComparer.OrdinalIgnoreCase);
                }

                var rows = pagedCustomers.Select(c =>
                {
                    ReadingSheet? reading = null;
                    bool hasReading = !string.IsNullOrWhiteSpace(c.Btno)
                                     && readingsByBtno.TryGetValue(c.Btno!, out reading);

                    return new AddReadingCustomerRow
                    {
                        Uid = c.Uid,
                        Btno = c.Btno,
                        CustomerName = c.CustomerName,
                        PloNo = c.PloNo,
                        Block = c.Block,
                        Sector = c.Sector,
                        Category = c.Category,
                        HasReading = hasReading,
                        PreviousReading = reading?.Previous1,
                        CurrentReading = reading?.Present1
                    };
                }).ToList();

                customers = new StaticPagedList<AddReadingCustomerRow>(
                    rows, pagedCustomers.PageNumber, pagedCustomers.PageSize, pagedCustomers.TotalItemCount);
            }

            ViewBag.Projects = projects;
            ViewBag.Blocks = blocks;
            ViewBag.SelectedProject = selectedProject;
            ViewBag.SelectedBlock = selectedBlock;
            ViewBag.BTNoSearch = btNoSearch;

            return View(customers);
        }

        [HttpGet]
        public JsonResult GetBlocksByProject(string project)
        {
            if (string.IsNullOrWhiteSpace(project))
            {
                return Json(new List<string>());
            }

            var trimProject = project.Trim();
            var blocks = _context.CustomersDetails
                .Where(c => c.Project != null
                            && c.Project.Trim() == trimProject
                            && c.Block != null
                            && c.Block.Trim() != "")
                .Select(c => c.Block!.Trim())
                .Distinct()
                .OrderBy(b => b)
                .ToList();

            return Json(blocks);
        }

        public IActionResult SearchCustomer(string sector = null, string searchTerm = null)
        {
            // Load all distinct sectors for the dropdown
            ViewBag.Sectors = _context.CustomersDetails
                                      .Select(c => c.Sector)
                                      .Distinct()
                                      .OrderBy(s => s)
                                      .ToList();

            // 🔍 PRIORITY 1: Text search - overrides dropdown filter if provided
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var searchResults = _context.CustomersDetails
                    .Where(c =>
                        (c.Btno ?? "").Contains(searchTerm) ||
                        (c.CustomerName ?? "").Contains(searchTerm) ||
                        (c.Cnicno ?? "").Contains(searchTerm) ||
                        (c.MobileNo ?? "").Contains(searchTerm))
                    .GroupBy(c => c.Sector)
                    .Select(g => new SectorCustomersViewModel
                    {
                        Sector = g.Key,
                        Customers = g.ToList()
                    })
                    .ToList();

                return View(searchResults);
            }

            // 🧭 PRIORITY 2: Dropdown filter if no search term is provided
            if (!string.IsNullOrEmpty(sector))
            {
                var groupedData = _context.CustomersDetails
                    .Where(c => c.Sector == sector)
                    .GroupBy(c => c.Sector)
                    .Select(g => new SectorCustomersViewModel
                    {
                        Sector = g.Key,
                        Customers = g.ToList()
                    })
                    .ToList();

                return View(groupedData);
            }

            // Default: No filters applied
            return View(new List<SectorCustomersViewModel>());
        }


    }






}

