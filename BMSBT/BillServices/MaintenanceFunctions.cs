using BMSBT.Models;
using BMSBT.Models.MyObjects;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BMSBT.BillServices
{
    public class MaintenanceFunctions
    {
        private readonly BmsbtContext _dbContext; // Replace with your DbContext class name
        private readonly OperatorDetailsService _operatorDetailsService;

        public MaintenanceFunctions(BmsbtContext dbContext)
        {
            _dbContext = dbContext;
        }










        public string GenerateBillForCustomer(int customerId, string currentBillingMonth, string currentBillingYear, string previousMonth, string previousYear, DateOnly? IssueDate, DateOnly? DueDate)
        {
            // Fetch customer details
            var customer = GetCustomerById(customerId);
            if (customer == null)
                return $"Customer with ID {customerId} not found.";

            // Check if the bill has already been generated
            if (IsBillAlreadyGenerated(customer, currentBillingMonth, currentBillingYear))
            {
                UpdateGeneratedMonthYear(customer, $"Bill already generated for {previousMonth} {previousYear}");
                return $"Bill already generated for customer {customer.CustomerName}.";
            }

            // Retrieve tariff details for the current billing period
            var tariff = GetTarrifDetails(customer, currentBillingMonth, currentBillingYear);
            if (tariff == null)
            {
                UpdateGeneratedMonthYear(customer, $"Tariff not found for {customer.Project} {customer.PlotType} {customer.Size}");
                return $"Tariff not found for customer {customer.CustomerName}.";
            }

            // Check previous bill and determine arrears
            decimal? arrearsAmount = 0;
            
            var previousBill = GetPreviousBill(customer, previousMonth, previousYear);

            if (previousBill == null)
            {
                if (!IsNewCustomer(customer))
                {
                    UpdateGeneratedMonthYear(customer, $"Previous bill not found. Previous Month: {previousMonth}");
                    return $"Previous bill not found for customer {customer.BTNo}.";
                }
            }
            else
            {
                // If bill exists, check if it's unpaid (NULL or 'unpaid')
                if (string.IsNullOrEmpty(previousBill.PaymentStatus) || previousBill.PaymentStatus.Equals("unpaid", StringComparison.OrdinalIgnoreCase))
                {
                    arrearsAmount = previousBill.BillAmountAfterDueDate;
                }
            }


            // Generate a new bill with arrears
            var newBill = CreateNewBill(customer, currentBillingMonth, currentBillingYear,
                                        Convert.ToDecimal(tariff.Charges), Convert.ToDecimal(tariff.Tax),
                                        IssueDate, DueDate, arrearsAmount); // Pass arrears

            // Assign an invoice number and update the status
            AssignInvoiceNo(newBill);

            // Update BillGenerationStatus with Month-Year
            customer.BillGenerationStatus = $"{currentBillingMonth}-{currentBillingYear}";

            UpdateGeneratedMonthYear(customer, $"Bill created for {currentBillingMonth} {currentBillingYear}");

            return $"Bill created successfully for customer {customer.CustomerName}.";
        }






        private CustomersMaintenance GetCustomerById(int customerId)
        {
            return _dbContext.CustomersMaintenance.FirstOrDefault(c => c.Uid == customerId);
        }




        public void GetPreviousBillingPeriod(string currentBillingMonth, string currentBillingYear)
        {
            // Map month numbers to their respective names
            var monthMap = new Dictionary<int, string>
                {
                  { 1, "January" }, { 2, "February" }, { 3, "March" },
                  { 4, "April" }, { 5, "May" }, { 6, "June" },
                  { 7, "July" }, { 8, "August" }, { 9, "September" },
                  { 10, "October" }, { 11, "November" }, { 12, "December" }
                };

            // Parse current month
            int currentMonth;
            if (!int.TryParse(currentBillingMonth, out currentMonth))
            {
                currentMonth = monthMap.FirstOrDefault(x => x.Value.Equals(currentBillingMonth, StringComparison.OrdinalIgnoreCase)).Key;
                if (currentMonth == 0)
                {
                    throw new ArgumentException($"Invalid month value: {currentBillingMonth}. Must be a valid integer or month name.");
                }
            }


            int currentYear;
            if (!int.TryParse(currentBillingYear, out currentYear))
            {
                throw new ArgumentException($"Invalid year value: {currentBillingYear}. Must be a valid integer.");
            }


            int previousMonth = currentMonth == 1 ? 12 : currentMonth - 1;
            int previousYear = currentMonth == 1 ? currentYear - 1 : currentYear;

            BillCreationState.PreviousMonth = monthMap[previousMonth];
            BillCreationState.PreviousYear = previousYear.ToString();

        }



        private MaintenanceTarrif GetTarrifDetails(CustomersMaintenance customer, string month, string year)
            {
                // Fetch the customer details based on the BTNo
                var customerDetail = _dbContext.CustomersMaintenance.FirstOrDefault(c => c.BTNo == customer.BTNo);

                // Return the matching maintenance tariff if customer details are found
                return _dbContext.MaintenanceTarrifs
                    .FirstOrDefault(t => customerDetail != null
                                         && t.PlotType == customerDetail.PlotType
                                         && t.Size == customerDetail.Size
                                         && t.Project == customerDetail.Project);
            }


        private MaintenanceBill? GetPreviousBill(CustomersMaintenance customer, string month, string year)
        {
            return _dbContext.MaintenanceBills
                .FirstOrDefault(b =>
                    b.Btno == customer.BTNo &&
                    b.BillingMonth == month &&
                    b.BillingYear == year);
        }


        public bool IsNewCustomer(CustomersMaintenance customer)
            {
                // Check if any maintenance bill exists for the given Btno
                bool billExists = _dbContext.MaintenanceBills.Any(b => b.Btno == customer.BTNo);

                // Return false if a bill exists, otherwise true
                return !billExists;
            }

        




        private bool IsBillAlreadyGenerated(CustomersMaintenance customer, string month, string year)
        {
            return _dbContext.MaintenanceBills.Any(b =>
                b.Btno == customer.BTNo && b.BillingMonth == month && b.BillingYear == year);
        }







        private MaintenanceBill CreateNewBill(
    CustomersMaintenance customer,
    string month,
    string year,
    decimal amount,
    decimal tax,
    DateOnly? IssueDate,
    DateOnly? DueDate,
    decimal? ArrearAmount)
        {
            // Convert inputs to decimal
            decimal amountDec = amount;
            decimal taxDec = tax;
            decimal actualArrearDec = ArrearAmount ?? 0m;

            // Look up Maintenance fines for this BTNo/FineMonth/FineYear
            int parsedYear;
            int.TryParse(year, out parsedYear);

            // Sum FineToCharge from Fine table (this is "Fine" for the bill)
            var fineTotalDec = _dbContext.Fine
                .Where(f =>
                    f.BTNo == customer.BTNo &&
                    f.FineMonth == month &&
                    f.FineYear == parsedYear &&
                    f.FineService == "Maintenance")
                .Select(f => (decimal?)f.FineToCharge)
                .Sum() ?? 0m;

            // --- New: Fetch Additional Charges (Water & Other) from AdditionalCharges table ---
            decimal waterCharges = _dbContext.AdditionalCharges
                .Where(a => a.BTNo == customer.BTNo && 
                           a.ServiceType == "Maintenance" && 
                           a.ChargesName == "Water Charges")
                .Select(a => a.ChargesAmount)
                .FirstOrDefault() ?? 0m;

            decimal otherCharges = _dbContext.AdditionalCharges
                .Where(a => a.BTNo == customer.BTNo && 
                           a.ServiceType == "Maintenance" && 
                           a.ChargesName == "Other Charges")
                .Select(a => a.ChargesAmount)
                .FirstOrDefault() ?? 0m;

            // 1) Bill due on‑time: BillAmountInDueDate = MaintCharges + TaxAmount + Arrears + Fine + WaterCharges + OtherCharges
            decimal billInDueDate = Math.Round(amountDec + taxDec + actualArrearDec + fineTotalDec + waterCharges + otherCharges, 0);

            // 2) 10% surcharge on (Charges + Tax)
            decimal baseChargesAndTax = amountDec + taxDec;
            decimal surcharge = Math.Round(baseChargesAndTax * 0.10m, 0);

            // 3) Bill after due date: BillAmountAfterDueDate = BillAmountInDueDate + BillSurcharge
            decimal billAfterDue = Math.Round(billInDueDate + surcharge, 0);

            // 4) Tax and arrears as whole numbers (rounded)
            decimal taxAmount = Math.Round(taxDec, 0);
            decimal arrearsAmt = Math.Round(actualArrearDec, 0);

            var newBill = new MaintenanceBill
            {
                CustomerNo = customer.CustomerNo,
                CustomerName = customer.CustomerName,
                Btno = customer.BTNo,
                BillingMonth = month,
                BillingYear = year,

                // Assign the rounded values - cast to int to match database column types
                BillAmountInDueDate = (int)billInDueDate,
                BillSurcharge = (int)surcharge,
                BillAmountAfterDueDate = (int)billAfterDue,
                TaxAmount = (int)taxAmount,
                Arrears = (int)arrearsAmt,
                MaintCharges = (int)amount,
                // Store Fine (sum of FineToCharge) into Fine column
                Fine = (int)fineTotalDec,
                WaterCharges = (int)waterCharges,
                OtherCharges = (int)otherCharges,
                IssueDate = IssueDate,
                DueDate = DueDate,

                PaymentStatus = "unpaid",
                LastUpdated = DateTime.Now,
                BillingDate = DateOnly.FromDateTime(DateTime.Now),
                MeterNo = customer.MeterNo,
                PaymentMethod = "NA",
                BankDetail = "NA",
                ValidDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(1)),
                InvoiceNo = null // Will be assigned later
            };

            _dbContext.MaintenanceBills.Add(newBill);
            _dbContext.SaveChanges();

            return newBill;
        }







        private void AssignInvoiceNo(MaintenanceBill newBill)
        {
            // Per Requirement: YYYYMM + Last 5 digits of CUSTOMERNO
            // Example: 202601 + 22306 = 20260122306
            var now = DateTime.Now;
            var datePart = now.ToString("yyyyMM");
            var cust = string.IsNullOrWhiteSpace(newBill.CustomerNo) ? "00000" : newBill.CustomerNo.Trim();
            
            // Get last 5 digits of customerNo
            var lastFive = cust.Length >= 5 ? cust[^5..] : cust.PadLeft(5, '0');
            
            newBill.InvoiceNo = $"{datePart}{lastFive}";
            _dbContext.Update(newBill);
            _dbContext.SaveChanges();
        }





        private void UpdateGeneratedMonthYear(CustomersMaintenance customer, string message)
        {
            customer.BillStatusMaint = message;
            _dbContext.Update(customer);
            _dbContext.SaveChanges();
        }





      

       


     
    }
}
