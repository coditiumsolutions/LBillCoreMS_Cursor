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
using BMSBT.ViewModels;

namespace BMSBT.Controllers
{
    public class ReadingController : Controller
    {
        private readonly BmsbtContext _context; // Your DbContext

        public ReadingController(BmsbtContext context)
        {
            _context = context;
        }


        public IActionResult Index(string search)
        {
            string currentMonth = DateTime.Now.ToString("MMMM");
            string currentYear = DateTime.Now.Year.ToString();

            ViewBag.TotalReadings = _context.ReadingSheets.Count();

            ViewBag.CurrentMonthReadings = _context.ReadingSheets
                .Count(r => r.Month == currentMonth && r.Year == currentYear);

            ViewBag.PendingReadings = _context.CustomersDetails
                .Count(c => !_context.ReadingSheets.Any(r => r.Btno == c.Btno && r.Month == currentMonth && r.Year == currentYear));

            return View();
        }


        public IActionResult Dashboard()
        {

            string currentMonth = DateTime.Now.ToString("MMMM");
            string currentYear = DateTime.Now.Year.ToString();

            ViewBag.TotalReadings = _context.ReadingSheets.Count();

            ViewBag.CurrentMonthReadings = _context.ReadingSheets
                .Count(r => r.Month == currentMonth && r.Year == currentYear);

            ViewBag.PendingReadings = _context.CustomersDetails
                .Count(c => !_context.ReadingSheets.Any(r => r.Btno == c.Btno && r.Month == currentMonth && r.Year == currentYear));

            return View();

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
            if (HttpContext.Session.GetInt32("UserName") == null)
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
            // Populate dropdowns
            ViewBag.BillingMonths = new List<string>
    {
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    };

            ViewBag.BillingYears = Enumerable.Range(DateTime.Now.Year - 5, 6)
                                             .Select(y => y.ToString())
                                             .ToList();

            var model = new ReadingSheet();

            if (!string.IsNullOrEmpty(btno))
            {
                var customer = _context.CustomersDetails.FirstOrDefault(c => c.Btno == btno);
                if (customer != null)
                {
                    model.Btno = customer.Btno;
                    model.CustomerNo = customer.CustomerNo;
                }
            }

            return View(model);
        }





        // POST: Create Reading
        [HttpPost]
        public IActionResult CreateReading(ReadingSheet model)
        {
            if (ModelState.IsValid)
            {
                bool exists = _context.ReadingSheets.Any(r => r.Btno == model.Btno
                                                              && r.Month == model.Month
                                                              && r.Year == model.Year);
                if (exists)
                {
                    ModelState.AddModelError("", "This reading already exists for the selected month and year.");
                }
                else if (model.Previous1 > model.Present1)
                {
                    ModelState.AddModelError("", "Previous Reading cannot be greater than Current Reading.");
                }
                else
                {
                    model.CreatedOn = DateTime.Now;
                    model.CreatedBy = HttpContext.Session.GetString("UserName") ?? "System";
                    model.History = $"Created on {model.CreatedOn:yyyy-MM-dd HH:mm} by {model.CreatedBy}";

                    _context.ReadingSheets.Add(model);
                    _context.SaveChanges();

                    TempData["SuccessMessage"] = "New reading added successfully!";
                    return RedirectToAction("Search", new { search = model.Btno });
                }
            }

            // ✅ Ensure dropdowns are populated on post-back
            ViewBag.BillingMonths = new List<string>
    {
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    };

            ViewBag.BillingYears = Enumerable.Range(DateTime.Now.Year - 5, 6)
                                             .Select(y => y.ToString())
                                             .ToList();

            return View(model);
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

