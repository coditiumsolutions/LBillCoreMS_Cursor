using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml; // EPPlus Namespace
using System.IO;
using System.Linq;
using BMSBT.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using X.PagedList.Extensions; // Replace with your namespace
//using BMSBT.Data;   // Replace with your DbContext namespace

namespace BMSBT.Controllers
{
    public class ReadingSheetController : Controller
    {
        private readonly BmsbtContext _context;

        public ReadingSheetController(BmsbtContext context)
        {
            _context = context;
        }

        public IActionResult Index(string selectedYear, string selectedMonth)
        {

            ViewBag.Years = new List<SelectListItem>
    {
        new SelectListItem { Value = "2024", Text = "2024" },
        new SelectListItem { Value = "2025", Text = "2025" }
    };

            ViewBag.Months = new List<SelectListItem>
    {
                 new SelectListItem { Value = "Janurary", Text = "Janurary" },
                 new SelectListItem { Value = "February", Text = "February" },
                 new SelectListItem { Value = "March", Text = "March" },
                 new SelectListItem { Value = "April", Text = "April" },
                 new SelectListItem { Value = "May", Text = "May" },
                 new SelectListItem { Value = "June", Text = "June" },
                 new SelectListItem { Value = "July", Text = "July" },
                 new SelectListItem { Value = "August", Text = "August" },
                 new SelectListItem { Value = "September", Text = "September" },
                 new SelectListItem { Value = "October", Text = "October" },
                 new SelectListItem { Value = "November", Text = "November" },
                 new SelectListItem { Value = "December", Text = "December" }
       
                
       
    };

            // Retain the selected values
            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedMonth = selectedMonth;


            // Check if both filters are provided
            if (string.IsNullOrEmpty(selectedYear) || string.IsNullOrEmpty(selectedMonth))
            {
                // Return empty data for the graph
                ViewBag.ChartLabels = new List<string>();
                ViewBag.ChartData = new List<int>();
                return View();
            }


            // ViewBag.Years = _context.ReadingSheets
            //.Select(r => r.Year)
            //.Distinct()
            //.OrderBy(y => y) // Ensure they are sorted
            //.ToList();

            //     ViewBag.Months = _context.ReadingSheets
            //         .Select(r => r.Month)
            //         .Distinct()
            //         .ToList();

            // Filter data based on selected year and month
            var filteredData = _context.ReadingSheets.AsQueryable();

            if (!string.IsNullOrEmpty(selectedYear))
            {
                filteredData = filteredData.Where(r => r.Year == selectedYear);
            }

            if (!string.IsNullOrEmpty(selectedMonth))
            {
                filteredData = filteredData.Where(r => r.Month == selectedMonth && r.Year == selectedYear);
            }

            // Generate the data for the graph
            var totalMeters = filteredData.Count();

            var readingSheetData = filteredData
                .GroupBy(c => c.MeterType)
                .Select(group => new
                {
                    meters = group.Key,
                    Total = group.Count()
                })
                .ToList();

            var totalAllSubProjects = readingSheetData.Sum(x => x.Total);

            // Prepare data for the chart
            ViewBag.ChartLabels = readingSheetData.Select(x => x.meters).ToList();
            ViewBag.ChartLabels.Add("All SubProjects"); // Add a label for the total
            ViewBag.ChartData = readingSheetData.Select(x => x.Total).ToList();
            ViewBag.ChartData.Add(totalMeters); // Add the total as a separate data point

            //// Pass selected filters back to the view
            //ViewBag.SelectedYear = selectedYear;
            //ViewBag.SelectedMonth = selectedMonth;

            return View();
        }


        public IActionResult ReadingDetails(int? page)
        {
            int pageNumber = page ?? 1;
            int pageSize = 10; // or any page size you prefer
            var data = _context.ReadingSheets.ToPagedList(pageNumber, pageSize);
            return View(data);
        }

        [HttpGet]
        public IActionResult UploadReading()
        {
            return View();
        }


        [HttpPost]
        public IActionResult UploadReading(IFormFile file)
        {
            if (file == null || file.Length <= 0)
            {
                ModelState.AddModelError("", "Please upload a valid Excel file.");
                return View();
            }

            // Check if the uploaded file is an Excel file
            var fileExtension = Path.GetExtension(file.FileName);
            if (fileExtension != ".xls" && fileExtension != ".xlsx")
            {
                ModelState.AddModelError("", "Only Excel files (.xls, .xlsx) are allowed.");
                return View();
            }

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Add this line
                using (var stream = new MemoryStream())
                {
                    file.CopyTo(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            ModelState.AddModelError("", "The uploaded file is empty.");
                            return View();
                        }

                        int rowCount = worksheet.Dimension.Rows;

                        // Start reading data from row 2 assuming row 1 contains column names
                        for (int row = 2; row <= rowCount; row++)
                        {
                            var readingSheet = new ReadingSheet
                            {

                                Btno = worksheet.Cells[row, 1].Text,
                                Year = "2025",
                                Month = "June",
                                
                                Previous1 = int.TryParse(worksheet.Cells[row, 11].Text, out int p1) ? p1 : (int?)null,
                                Present1 = int.TryParse(worksheet.Cells[row, 2].Text, out int pr1) ? pr1 : (int?)null,
                                
                                Previous2 = int.TryParse(worksheet.Cells[row, 13].Text, out int p2) ? p2 : (int?)null,
                                Present2 = int.TryParse(worksheet.Cells[row, 3].Text, out int pr2) ? pr2 : (int?)null,
                                
                                Previous3 = int.TryParse(worksheet.Cells[row, 15].Text, out int p3) ? p3 : (int?)null,
                                Present3 = int.TryParse(worksheet.Cells[row, 4].Text, out int pr3) ? pr3 : (int?)null,
                               
                            };




                            // Check for duplicates before adding to the database context
                            var existingRecord = _context.ReadingSheets
                                .Any(r => r.Btno == readingSheet.Btno &&
                                          r.Year == readingSheet.Year &&
                                          r.Month == readingSheet.Month);

                            if (!existingRecord)
                            {
                                _context.ReadingSheets.Add(readingSheet);
                            }
                            else
                            {
                                Console.WriteLine($"Duplicate Record Found: BTNo = {readingSheet.Btno}, Year = {readingSheet.Year}, Month = {readingSheet.Month}");
                            }
                        }


                        try
                        {
                            _context.SaveChanges();
                            TempData["Message"] = "File uploaded and data saved successfully!";
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError("", $"An error occurred while saving data: {ex.InnerException?.Message ?? ex.Message}");
                            Console.WriteLine($"Error: {ex.InnerException?.Message ?? ex.Message}");
                        }


                        // Save changes to database
                        _context.SaveChanges();
                        TempData["Message"] = "File uploaded and data saved successfully!";
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error: {ex.Message}");
            }

            return View();
        }


        [HttpGet]
        public IActionResult Edit(int id)
        {
            var reading = _context.ReadingSheets.Find(id);
            if (reading == null)
            {
                return NotFound();
            }
            return View(reading);
        }

        [HttpPost]
       
        public IActionResult Edit( ReadingSheet model)
        {
            

           
                try
                {
                    _context.Update(model);
                    _context.SaveChanges();
                    return RedirectToAction("ReadingDetails"); // Redirect back to the list page
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while updating the record.");
                }
            

            return View(model);
        }



      




    }
}
