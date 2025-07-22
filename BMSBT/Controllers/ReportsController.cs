using BMSBT.Models;
using BMSBT.Roles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Data.SqlClient;

namespace BMSBT.Controllers
{
   
    public class ReportsController : Controller
    {
        private readonly BmsbtContext _dbContext;
        public ReportsController(BmsbtContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.Username = HttpContext.Session.GetString("UserName");
            ViewBag.LoginTime = HttpContext.Session.GetString("LoginTime");
            return View();
        }











        public IActionResult ExportToExcel()
        {
            // Fetch data from your DbContext
            var bills = _dbContext.SpecialDiscountBills.ToList();

            using (var package = new ExcelPackage())
            {
                // Add a worksheet to the package
                var worksheet = package.Workbook.Worksheets.Add("Special Discount Bills");

                // Add headers for all columns
                worksheet.Cells[1, 1].Value = "BT No";
                worksheet.Cells[1, 2].Value = "Customer Name";
                worksheet.Cells[1, 3].Value = "Project";
                worksheet.Cells[1, 4].Value = "Block";
                worksheet.Cells[1, 5].Value = "Sector";
                worksheet.Cells[1, 6].Value = "Plo No";
                worksheet.Cells[1, 7].Value = "Bill Amount";
                worksheet.Cells[1, 8].Value = "Invoice No";
                worksheet.Cells[1, 9].Value = "Billing Month";
                worksheet.Cells[1, 10].Value = "Billing Year";
                worksheet.Cells[1, 11].Value = "Billing Date";
                worksheet.Cells[1, 12].Value = "Due Date";
                worksheet.Cells[1, 13].Value = "Reading Date";
                worksheet.Cells[1, 14].Value = "Issue Date";
                worksheet.Cells[1, 15].Value = "Valid Date";
                worksheet.Cells[1, 16].Value = "Meter Type";
                worksheet.Cells[1, 17].Value = "Meter No";
                worksheet.Cells[1, 18].Value = "Payment Status";
                worksheet.Cells[1, 19].Value = "Amount Paid";
                worksheet.Cells[1, 20].Value = "Payment Date";
                worksheet.Cells[1, 21].Value = "Payment Method";
                worksheet.Cells[1, 22].Value = "Bank Detail";
                worksheet.Cells[1, 23].Value = "Energy Cost";
                worksheet.Cells[1, 24].Value = "Last Updated";
                worksheet.Cells[1, 25].Value = "Bill Amount in Due Date";
                worksheet.Cells[1, 26].Value = "Bill Surcharge";
                worksheet.Cells[1, 27].Value = "Bill Amount After Due Date";
                worksheet.Cells[1, 28].Value = "Previous Reading 1";
                worksheet.Cells[1, 29].Value = "Current Reading 1";
                worksheet.Cells[1, 30].Value = "Difference 1";
                worksheet.Cells[1, 31].Value = "Previous Reading 2";
                worksheet.Cells[1, 32].Value = "Current Reading 2";
                worksheet.Cells[1, 33].Value = "Difference 2";
                worksheet.Cells[1, 34].Value = "Previous Solar Reading";
                worksheet.Cells[1, 35].Value = "Current Solar Reading";
                worksheet.Cells[1, 36].Value = "Difference Solar";
                worksheet.Cells[1, 37].Value = "Total Unit";
                worksheet.Cells[1, 38].Value = "Opc";
                worksheet.Cells[1, 39].Value = "GST";
                worksheet.Cells[1, 40].Value = "Ptv Fee";
                worksheet.Cells[1, 41].Value = "Further Tax";
                worksheet.Cells[1, 42].Value = "Arrears";
                worksheet.Cells[1, 43].Value = "History";

                // Add data rows
                int row = 2;
                foreach (var bill in bills)
                {
                    worksheet.Cells[row, 1].Value = bill.BTNo;
                    worksheet.Cells[row, 2].Value = bill.CustomerName;
                    worksheet.Cells[row, 3].Value = bill.Project;
                    worksheet.Cells[row, 4].Value = bill.Block;
                    worksheet.Cells[row, 5].Value = bill.Sector;
                    worksheet.Cells[row, 6].Value = bill.PloNo;
                    worksheet.Cells[row, 7].Value = bill.BillAmount;
                    worksheet.Cells[row, 8].Value = bill.InvoiceNo;
                    worksheet.Cells[row, 9].Value = bill.BillingMonth;
                    worksheet.Cells[row, 10].Value = bill.BillingYear;
                    worksheet.Cells[row, 11].Value = bill.BillingDate?.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 12].Value = bill.DueDate?.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 13].Value = bill.ReadingDate?.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 14].Value = bill.IssueDate?.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 15].Value = bill.ValidDate?.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 16].Value = bill.MeterType;
                    worksheet.Cells[row, 17].Value = bill.MeterNo;
                    worksheet.Cells[row, 18].Value = bill.PaymentStatus;
                    worksheet.Cells[row, 19].Value = bill.AmountPaid;
                    worksheet.Cells[row, 20].Value = bill.PaymentDate?.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 21].Value = bill.PaymentMethod;
                    worksheet.Cells[row, 22].Value = bill.BankDetail;
                    worksheet.Cells[row, 23].Value = bill.EnergyCoast;
                    worksheet.Cells[row, 24].Value = bill.LastUpdated?.ToString("yyyy-MM-dd HH:mm:ss");
                    worksheet.Cells[row, 25].Value = bill.BillAmountInDueDate;
                    worksheet.Cells[row, 26].Value = bill.BillSurcharge;
                    worksheet.Cells[row, 27].Value = bill.BillAmountAfterDueDate;
                    worksheet.Cells[row, 28].Value = bill.PreviousReading1;
                    worksheet.Cells[row, 29].Value = bill.CurrentReading1;
                    worksheet.Cells[row, 30].Value = bill.Difference1;
                    worksheet.Cells[row, 31].Value = bill.PreviousReading2;
                    worksheet.Cells[row, 32].Value = bill.CurrentReading2;
                    worksheet.Cells[row, 33].Value = bill.Difference2;
                    worksheet.Cells[row, 34].Value = bill.PreviousSolarReading;
                    worksheet.Cells[row, 35].Value = bill.CurrentSolarReading;
                    worksheet.Cells[row, 36].Value = bill.DifferenceSolar;
                    worksheet.Cells[row, 37].Value = bill.TotalUnit;
                    worksheet.Cells[row, 38].Value = bill.Opc;
                    worksheet.Cells[row, 39].Value = bill.Gst;
                    worksheet.Cells[row, 40].Value = bill.Ptvfee;
                    worksheet.Cells[row, 41].Value = bill.Furthertax;
                    worksheet.Cells[row, 42].Value = bill.Arrears;
                    worksheet.Cells[row, 43].Value = bill.History;

                    row++;
                }

                // Save the package as a byte array and return it as a file
                var fileContent = package.GetAsByteArray();
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SpecialDiscountBills.xlsx");
            }
        }









        public async Task<IActionResult> GetBillsSpecialDiscount()
        {
            return View();   // Create the parameters using SqlParameter
        }

        [HttpPost]
        public async Task<IActionResult> GetBillsSpecialDiscount(string billingMonth, string billingYear, string project = null)
        {
            // Create the parameters using Microsoft.Data.SqlClient.SqlParameter
            var billingMonthParam = new Microsoft.Data.SqlClient.SqlParameter("@BillingMonth", billingMonth);
            var billingYearParam = new Microsoft.Data.SqlClient.SqlParameter("@BillingYear", billingYear);
            var projectParam = new Microsoft.Data.SqlClient.SqlParameter("@Project", project ?? (object)DBNull.Value);

            // Execute the stored procedure using FromSqlRaw with parameters
            var bills = await _dbContext.SpecialDiscountBills.FromSqlRaw(
                "EXEC dbo.usp_GetBillsSpecialDiscount @BillingMonth, @BillingYear, @Project",
                billingMonthParam, billingYearParam, projectParam)
                .ToListAsync();

            return View(bills); // Return the result to the view
        }




        public async Task<IActionResult> GetBillsRecoverable()
        {
            return View();   // Create the parameters using SqlParameter
        }

        [HttpPost]
        public async Task<IActionResult> GetBillsRecoverable(string billingMonth, string billingYear, string project = null)
        {
            // Create the parameters using Microsoft.Data.SqlClient.SqlParameter
            var billingMonthParam = new Microsoft.Data.SqlClient.SqlParameter("@BillingMonth", billingMonth);
            var billingYearParam = new Microsoft.Data.SqlClient.SqlParameter("@BillingYear", billingYear);
            var projectParam = new Microsoft.Data.SqlClient.SqlParameter("@Project", project ?? (object)DBNull.Value);

            // Execute the stored procedure using FromSqlRaw with parameters
            var bills = await _dbContext.SpecialDiscountBills.FromSqlRaw(
                "EXEC dbo.usp_GetBillsExcludingSpecialDiscount @BillingMonth, @BillingYear, @Project",
                billingMonthParam, billingYearParam, projectParam)
                .ToListAsync();

            return View(bills); // Return the result to the view
        }










        public async Task<IActionResult> GetTwoMonthOutstandingBills()
        {
            return View();
        }

        // POST: Execute the stored procedure and display results
        [HttpPost]
        public async Task<IActionResult> GetTwoMonthOutstandingBills(string billingMonth1, string billingMonth2, string billingYear, string project = null)
        {
            try
            {
                // Validate required parameters
                if (string.IsNullOrEmpty(billingMonth1) || string.IsNullOrEmpty(billingMonth2) || string.IsNullOrEmpty(billingYear))
                {
                    ModelState.AddModelError("", "Billing Month 1, Billing Month 2, and Billing Year are required.");
                    return View();
                }

                // Create the parameters using Microsoft.Data.SqlClient.SqlParameter
                var billingMonth1Param = new Microsoft.Data.SqlClient.SqlParameter("@BillingMonth1", billingMonth1);
                var billingMonth2Param = new Microsoft.Data.SqlClient.SqlParameter("@BillingMonth2", billingMonth2);
                var billingYearParam = new Microsoft.Data.SqlClient.SqlParameter("@BillingYear", billingYear);
                var projectParam = new Microsoft.Data.SqlClient.SqlParameter("@Project", project ?? (object)DBNull.Value);

                // Execute the stored procedure using FromSqlRaw with parameters
                var bills = await _dbContext.TwoMonthOutstandingBills.FromSqlRaw(
                    "EXEC dbo.usp_GetTwoMonthOutstandingBills @BillingMonth1, @BillingMonth2, @BillingYear, @Project",
                    billingMonth1Param, billingMonth2Param, billingYearParam, projectParam)
                    .ToListAsync();

                // Set ViewBag for search performed flag
                ViewBag.SearchPerformed = true;
                ViewBag.BillingMonth1 = billingMonth1;
                ViewBag.BillingMonth2 = billingMonth2;
                ViewBag.BillingYear = billingYear;
                ViewBag.Project = project;

                return View(bills); // Return the result to the view
            }
            catch (Exception ex)
            {
                // Log the exception (you should use your logging framework here)
                // _logger.LogError(ex, "Error executing GetTwoMonthOutstandingBills");

                ModelState.AddModelError("", "An error occurred while retrieving the bills. Please try again.");
                return View();
            }
        }

        // Optional: Export to Excel action
        [HttpPost]
        public async Task<IActionResult> ExportTwoMonthOutstandingBillsToExcel(string billingMonth1, string billingMonth2, string billingYear, string project = null)
        {
            try
            {
                // Validate required parameters
                if (string.IsNullOrEmpty(billingMonth1) || string.IsNullOrEmpty(billingMonth2) || string.IsNullOrEmpty(billingYear))
                {
                    return BadRequest("All required fields must be provided.");
                }

                // Use FormattableString to avoid SqlParameter issues
                var sql = $"EXEC dbo.usp_GetTwoMonthOutstandingBills {billingMonth1}, {billingMonth2}, {billingYear}, {project ?? "NULL"}";

                // Or use object array approach (safer)
                var bills = await _dbContext.TwoMonthOutstandingBills
                    .FromSqlRaw("EXEC dbo.usp_GetTwoMonthOutstandingBills {0}, {1}, {2}, {3}",
                        billingMonth1, billingMonth2, billingYear, project ?? (object)DBNull.Value)
                    .ToListAsync();

                if (!bills.Any())
                {
                    return BadRequest("No data found for the selected criteria.");
                }

                // Set EPPlus license context
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Outstanding Bills");

                // Style the header
                using (var range = worksheet.Cells[1, 1, 1, 10])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thick);
                }

                // Header
                var headers = new string[]
                {
            "BT No", "Customer Name", "Project", "Block", "Sector",
            "Plot No", "Months Outstanding", "Total Bill", "Total Paid", "Outstanding Amount"
                };

                for (int col = 0; col < headers.Length; col++)
                {
                    worksheet.Cells[1, col + 1].Value = headers[col];
                }

                // Data
                for (int i = 0; i < bills.Count; i++)
                {
                    var row = i + 2;
                    var bill = bills[i];

                    worksheet.Cells[row, 1].Value = bill.BTNo;
                    worksheet.Cells[row, 2].Value = bill.CustomerName;
                    worksheet.Cells[row, 3].Value = bill.Project;
                    worksheet.Cells[row, 4].Value = bill.Block;
                    worksheet.Cells[row, 5].Value = bill.Sector;
                    worksheet.Cells[row, 6].Value = bill.PloNo;
                    worksheet.Cells[row, 7].Value = bill.MonthsOutstanding;
                    worksheet.Cells[row, 8].Value = bill.TotalBill;
                    worksheet.Cells[row, 9].Value = bill.TotalPaid;
                    worksheet.Cells[row, 10].Value = bill.TotalOutstanding;
                }

                // Format currency columns
                worksheet.Cells[2, 8, bills.Count + 1, 10].Style.Numberformat.Format = "#,##0.00";

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                // Generate the Excel file
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream);
                stream.Position = 0;

                string excelName = $"TwoMonthOutstandingBills_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error exporting to Excel: {ex.Message}");
            }
        }



    }
}
