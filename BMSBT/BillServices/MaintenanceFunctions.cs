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
            int? arrearsAmount = 0;
            
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
                // If bill exists, check if it's unpaid
                if (previousBill.PaymentStatus=="unpaid") // make sure IsPaid exists in your MaintenanceBill model
                {
                    arrearsAmount = previousBill.BillAmountAfterDueDate; // or .BillAmount, depending on your schema
                }
            }


            // Generate a new bill with arrears
            var newBill = CreateNewBill(customer, currentBillingMonth, currentBillingYear,
                                        Convert.ToDecimal(tariff.Charges), Convert.ToDecimal(tariff.Tax),
                                        IssueDate, DueDate, arrearsAmount); // Pass arrears

            // Assign an invoice number and update the status
            AssignInvoiceNo(newBill);
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
    int? ArrearAmount)
        {
            // Convert inputs to decimal
            decimal amountDec = amount;
            decimal taxDec = tax;
            decimal actualArrearDec = ArrearAmount ?? 0m;

            // 1) Bill due on‑time (including arrears), rounded
            decimal billInDueDate = Math.Round(
                amountDec + taxDec + actualArrearDec
            , 0);

            // 2) 10% surcharge on the in‑due date bill, rounded
            decimal surcharge = Math.Round(
                billInDueDate * 0.10m
            , 0);

            // 3) Bill after due date (in‑due bill + surcharge), rounded
            decimal billAfterDue = Math.Round(
                billInDueDate + surcharge
            , 0);

            // 4) Tax and arrears as whole numbers (rounded)
            int taxAmount = (int)Math.Round(taxDec, 0);
            int arrearsAmt = (int)Math.Round(actualArrearDec, 0);

            var newBill = new MaintenanceBill
            {
                CustomerNo = customer.CustomerNo,
                CustomerName = customer.CustomerName,
                Btno = customer.BTNo,
                BillingMonth = month,
                BillingYear = year,

                // Assign the rounded values
                BillAmountInDueDate = (int)billInDueDate,
                BillSurcharge = (int)surcharge,
                BillAmountAfterDueDate = (int)billAfterDue,
                TaxAmount = taxAmount,
                Arrears = arrearsAmt,
                MaintCharges=amount,
                IssueDate = IssueDate,
                DueDate = DueDate,

                PaymentStatus = "Unpaid",
                LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                BillingDate = DateOnly.FromDateTime(DateTime.Now),
                MeterNo = customer.MeterNo,
                PaymentMethod = "N/A",
                BankDetail = "N/A",
                ValidDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(1)),
                InvoiceNo = null // Will be assigned later
            };

            _dbContext.MaintenanceBills.Add(newBill);
            _dbContext.SaveChanges();

            return newBill;
        }







        private void AssignInvoiceNo(MaintenanceBill newBill)
        {
            string month = DateTime.Now.ToString("MM");
            string year = DateTime.Now.ToString("yy");
            string paddedUid = newBill.Uid.ToString().PadLeft(8, '0');
            newBill.InvoiceNo = $"{month}{year}{paddedUid}";
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
