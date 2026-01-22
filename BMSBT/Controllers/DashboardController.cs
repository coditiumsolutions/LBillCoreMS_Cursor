using BMSBT.DTO;
using BMSBT.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.SqlClient;

namespace BMSBT.Controllers
{
    public class DashboardController : Controller
    {
       
            private readonly BmsbtContext _dbContext;
            private readonly ICurrentOperatorService _operatorService;
            public DashboardController(BmsbtContext dbContext, ICurrentOperatorService operatorService)
            {
                _dbContext = dbContext;
                _operatorService = operatorService;
            }


        public ActionResult EDashboard(string month, string year, string project)
        {
            var model = new DashboardViewModel();

            // Get the current month and year
            string currentMonth = DateTime.Now.ToString("MMMM");
            string currentYear = DateTime.Now.Year.ToString();

            // Assign default values if parameters are null or empty
            month = string.IsNullOrEmpty(month) ? currentMonth : month;
            year = string.IsNullOrEmpty(year) ? currentYear : year;

            try
            {
                // Initialize lists
                model.Projects = new List<string>();
                model.Years = new List<string>();
                model.Months = new List<string>();

                // Get the formatted billing report data
                var billingReportQuery = @"EXEC sp_GetBillingReportFormatted @Month = {0}, @Year = {1}, @Project = {2}";

                // Execute the stored procedure and get results
                var billingReportData = _dbContext.BillingReportData
                    .FromSqlRaw(billingReportQuery,
                        string.IsNullOrEmpty(month) ? (object)DBNull.Value : month,
                        string.IsNullOrEmpty(year) ? (object)DBNull.Value : year,
                        string.IsNullOrEmpty(project) ? (object)DBNull.Value : project)
                    .ToList();

                // Process the data to populate model properties
                if (billingReportData != null && billingReportData.Any())
                {
                    // Extract data based on sections
                    var generatedDetail = billingReportData.Where(x => x.Section == "Generated Detail").ToList();
                    var netMeteringDetail = billingReportData.Where(x => x.Section == "Generated Detail - Net Metering").ToList();
                    var paymentsDetail = billingReportData.Where(x => x.Section == "Payments Detail").ToList();

                    // Populate Generated Detail
                    var billsGenerated = generatedDetail.FirstOrDefault(x => x.Metric == "Bills Generated");
                    var totalCurrentBilling = generatedDetail.FirstOrDefault(x => x.Metric == "Total Current Billing (Rs)");

                    if (billsGenerated != null)
                    {
                        model.TotalBillsGenerated = Convert.ToInt32(billsGenerated.Value);
                        model.BillsUnits = int.TryParse(billsGenerated.SecondaryValue, out int units) ? units : 0;
                    }

                    if (totalCurrentBilling != null)
                    {
                        model.TotalBillAmountGenerated = totalCurrentBilling.Value;
                        // Don't overwrite BillsUnits if it's already set from the first record
                        if (model.BillsUnits == 0)
                        {
                            model.BillsUnits = int.TryParse(totalCurrentBilling.SecondaryValue, out int units) ? units : 0;
                        }
                    }

                    // Populate Net Metering Detail
                    var netMeterBillsGenerated = netMeteringDetail.FirstOrDefault(x => x.Metric == "Bills Generated");
                    var netMeterTotalBilling = netMeteringDetail.FirstOrDefault(x => x.Metric == "Total Current Billing (Rs)");

                    if (netMeterBillsGenerated != null)
                    {
                        model.NetMeterBillsGenerated = Convert.ToInt32(netMeterBillsGenerated.Value);
                        model.NetMeterBillsUnits = int.TryParse(netMeterBillsGenerated.SecondaryValue, out int units) ? units : 0;
                    }

                    if (netMeterTotalBilling != null)
                    {
                        model.NetMeterTotalBilling = netMeterTotalBilling.Value;
                    }

                    // Populate Payments Detail
                    var billPaidAmount = paymentsDetail.FirstOrDefault(x => x.Metric == "Bill Paid (Amount)");
                    var paidBillsCount = paymentsDetail.FirstOrDefault(x => x.Metric == "Paid (No Of Bills)");

                    if (billPaidAmount != null)
                    {
                        model.TotalBillAmountCollected = billPaidAmount.Value;
                        // SecondaryValue for "Bill Paid (Amount)" row contains unpaid amount as STRING/INT
                        model.BillUnpaidAmount = decimal.TryParse(billPaidAmount.SecondaryValue, out decimal unpaidAmt) ? unpaidAmt : 0;
                    }

                    if (paidBillsCount != null)
                    {
                        model.TotalBillsPaid = Convert.ToInt32(paidBillsCount.Value);
                        model.UnpaidBillsCount = int.TryParse(paidBillsCount.SecondaryValue, out int unpaidCount) ? unpaidCount : 0;
                    }
                }

                // Store the complete billing report data for the view
                model.BillingReportData = billingReportData;

                // Store selected values in ViewBag to retain in UI
                ViewBag.SelectedMonth = month;
                ViewBag.SelectedYear = year;
                ViewBag.SelectedProject = project;
                ViewBag.BillingPeriod = $"{year} - {month}";

                return View(model);
            }
            catch (Exception ex)
            {
                // Log the exception (add logging here)
                ModelState.AddModelError("", $"An error occurred while loading the dashboard data: {ex.Message}");
                return View(new DashboardViewModel());
            }
        }




        //public ActionResult MDashboard(string month, string year, string project, string subProject, string sector, string block)
        //    {
        //        var model = new DashboardViewModel();

        //        // Get the current month and year
        //        string currentMonth = DateTime.Now.ToString("MMMM"); // Example: "March"
        //        string currentYear = DateTime.Now.Year.ToString();

        //        // Assign default values if parameters are null or empty
        //        month = string.IsNullOrEmpty(month) ? currentMonth : month;
        //        year = string.IsNullOrEmpty(year) ? currentYear : year;

        //        // Fetch dropdown data
        //        model.Projects = _dbContext.CustomersDetails
        //                            .Where(c => c.Project != null)
        //                            .Select(c => c.Project)
        //                            .Distinct()
        //                            .ToList();

        //        model.Sectors = _dbContext.CustomersDetails
        //                          .Where(c => c.Sector != null)
        //                          .Select(c => c.Sector)
        //                          .Distinct()
        //                          .ToList();

        //        model.Blocks = _dbContext.CustomersDetails
        //                         .Where(c => c.Block != null)
        //                         .Select(c => c.Block)
        //                         .Distinct()
        //                         .ToList();

        //        model.Years = _dbContext.ElectricityBills
        //                        .Where(e => e.BillingYear != null)
        //                        .Select(e => e.BillingYear)
        //                        .Distinct()
        //                        .ToList();

        //        model.Months = _dbContext.ElectricityBills
        //                         .Where(e => e.BillingMonth != null)
        //                         .Select(e => e.BillingMonth)
        //                         .Distinct()
        //                         .ToList();

        //        // Fetching dashboard statistics with filtering
        //        var query = from bill in _dbContext.MaintenanceBills
        //                    join customer in _dbContext.CustomersDetails on bill.Btno equals customer.Btno
        //                    select new { bill, customer };

        //        // Apply filters
        //        if (!string.IsNullOrEmpty(month))
        //        {
        //            query = query.Where(x => x.bill.BillingMonth == month);
        //        }

        //        if (!string.IsNullOrEmpty(year))
        //        {
        //            query = query.Where(x => x.bill.BillingYear == year);
        //        }

        //        if (!string.IsNullOrEmpty(project))
        //        {
        //            query = query.Where(x => x.customer.Project == project);
        //        }

        //        if (!string.IsNullOrEmpty(subProject))
        //        {
        //            query = query.Where(x => x.customer.SubProject == subProject);
        //        }

        //        if (!string.IsNullOrEmpty(sector))
        //        {
        //            query = query.Where(x => x.customer.Sector == sector);
        //        }

        //        if (!string.IsNullOrEmpty(block))
        //        {
        //            query = query.Where(x => x.customer.Block == block);
        //        }

        //        // Assign filtered data
        //        model.TotalBillsGenerated = query.Count();
        //        model.TotalBillsPaid = query.Count(x => x.bill.PaymentStatus == "Paid");
        //        model.TotalBillAmountGenerated = query.Sum(x => (decimal?)x.bill.BillAmountInDueDate) ?? 0;
        //        model.TotalBillAmountCollected = query.Where(x => x.bill.PaymentStatus == "Paid")
        //                                              .Sum(x => (decimal?)x.bill.BillAmountInDueDate) ?? 0;

        //        // Populate the table with filtered data
        //        model.BillingData = query.Select(x => new BillingDataViewModel
        //        {
        //            Project = x.customer.Project,
        //            SubProject = x.customer.SubProject,
        //            Sector = x.customer.Sector,
        //            Block = x.customer.Block,
        //            BillingMonth = x.bill.BillingMonth,
        //            TotalBillsGenerated = query.Count(y => y.customer.Project == x.customer.Project
        //                                                && y.customer.SubProject == x.customer.SubProject
        //                                                && y.customer.Sector == x.customer.Sector
        //                                                && y.customer.Block == x.customer.Block),
        //            TotalBillsPaid = query.Count(y => y.customer.Project == x.customer.Project
        //                                           && y.customer.SubProject == x.customer.SubProject
        //                                           && y.customer.Sector == x.customer.Sector
        //                                           && y.customer.Block == x.customer.Block
        //                                           && y.bill.PaymentStatus == "Paid"),
        //            TotalBillAmountGenerated = query.Where(y => y.customer.Project == x.customer.Project
        //                                                     && y.customer.SubProject == x.customer.SubProject
        //                                                     && y.customer.Sector == x.customer.Sector
        //                                                     && y.customer.Block == x.customer.Block)
        //                                            .Sum(y => (decimal?)y.bill.BillAmountInDueDate) ?? 0,
        //            TotalBillAmountCollected = query.Where(y => y.customer.Project == x.customer.Project
        //                                                     && y.customer.SubProject == x.customer.SubProject
        //                                                     && y.customer.Sector == x.customer.Sector
        //                                                     && y.customer.Block == x.customer.Block
        //                                                     && y.bill.PaymentStatus == "Paid")
        //                                            .Sum(y => (decimal?)y.bill.BillAmountInDueDate) ?? 0
        //        }).Distinct().ToList();

        //        // Store selected values in ViewBag to retain in UI
        //        ViewBag.SelectedMonth = month;
        //        ViewBag.SelectedYear = year;
        //        ViewBag.Projects = project;
        //        ViewBag.SubProjects = subProject;
        //        ViewBag.Sectors = sector;
        //        ViewBag.Blocks = block;

        //        return View(model);
        //    }

    }
    }