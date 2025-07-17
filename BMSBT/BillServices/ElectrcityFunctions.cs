using BMSBT.Models.MyObjects;
using BMSBT.Models;
using System.Data.Entity;
using Microsoft.VisualBasic;
using DevExpress.XtraRichEdit.Commands;
using DevExpress.Xpo;

namespace BMSBT.BillServices
{
    public class ElectrcityFunctions
    {

        private readonly BmsbtContext dbContext;


        public ElectrcityFunctions(BmsbtContext _dbContext)
        {
            dbContext = _dbContext;
        }





        public string GenerateEBillForCustomer(int customerId, string currentBillingMonth, string currentBillingYear, string PreviousMonth, string previousYear, DateOnly? IssueDate, DateOnly? DueDate, DateOnly? ValidDate,string UserName ,string FPAMONTH1,string FPAYEAR1,decimal? FPARATE1, string FPAMONTH2, string FPAYEAR2, decimal? FPARATE2)
        {
            double? arrears = 0;
            double? FPACHAR = 0;
            var customer = dbContext.CustomersDetails.FirstOrDefault(c => c.Uid == customerId);
            if (customer == null)
            {

                return $"Customer with ID {customerId} not found.";
            }




            //Duplication Check 
            bool billAlreadyExists = dbContext.ElectricityBills.Any(b =>
              b.Btno == customer.Btno &&
              b.BillingMonth == currentBillingMonth &&
              b.BillingYear == currentBillingYear);

            if (billAlreadyExists)
            {
                customer.BillStatus = $"Bill Already Generated for {currentBillingMonth} {currentBillingYear}";
                dbContext.Update(customer);
                dbContext.SaveChanges();
                return $"Bill already generated for customer {customer.CustomerName}.";
            }



            // Arrear Calcukation Here 
            int billsCount = dbContext.ElectricityBills.Count(b => b.Btno == customer.Btno);
            if (billsCount > 0)
            {
                var previousBill = dbContext.ElectricityBills.FirstOrDefault(b =>
                    b.Btno == customer.Btno &&
                    b.BillingMonth == PreviousMonth &&
                    b.BillingYear == previousYear);

                if (previousBill != null)
                {
                    var billStatus = previousBill.PaymentStatus;
                    if (billStatus == "Partially Paid" || billStatus=="partially paid")
                    { 
                      arrears = Convert.ToDouble(previousBill.BillAmountInDueDate) - Convert.ToDouble(previousBill.AmountPaid); 
                    }
                   
                    else if (billStatus == "UnPaid" || billStatus== "unpaid")
                    {
                        arrears = Convert.ToDouble(previousBill.BillAmountAfterDueDate) ;
                    }
                    else
                    {
                        arrears = 0;
                    }
                }
                else
                {
                    customer.BillStatus = $"No bill found for {PreviousMonth} {previousYear}.";
                    dbContext.Update(customer);
                    dbContext.SaveChanges();
                    return $"No previous bill found for {PreviousMonth} {previousYear} for customer {customer.CustomerName}.";
                }
            }




            //Reading Fetch from Db 

            var reading = dbContext.ReadingSheets.FirstOrDefault(r =>
                r.Btno == customer.Btno &&
                r.Month == currentBillingMonth &&
                r.Year == currentBillingYear);

            if (reading == null)
            {
                customer.BillStatus = $"Reading sheet not found for {currentBillingMonth} {currentBillingYear}";
                dbContext.Update(customer);
                dbContext.SaveChanges();
                return $"Reading sheet not found for customer {customer.CustomerName}.";
            }

            //Units Diffrence calculation by reading subtraction Here 
            int unitDifference1 =Convert.ToInt32(reading.Present1) - Convert.ToInt32(reading.Previous1);
            decimal ? TotalUNits  = unitDifference1;

            if (reading.Previous1 < 0 || reading.Present1 < 0)
            {
                customer.BillStatus = $"Previous Or Present Reading cannot be Negative for {currentBillingMonth} {currentBillingYear}";
                dbContext.Update(customer);
                dbContext.SaveChanges();
                return $"Wrong Reading for customer {customer.CustomerName}.";
            }



            if (unitDifference1 < 0)
            {
                customer.BillStatus = $"Previous Reading cannot be Less Than Current Reading for {currentBillingMonth} {currentBillingYear}";
                dbContext.Update(customer);
                dbContext.SaveChanges();
                return $"Wrong Reading for customer {customer.CustomerName}.";
            }



            //Tarrif rate multiply units  to calculate Bill Amount 

            var tariff = dbContext.Tarrifs.AsNoTracking()
                        .FirstOrDefault(t => t.TarrifType == customer.Category);

            if (tariff == null)
            {
                customer.BillStatus = $"Tariff Not Found for {currentBillingMonth} {currentBillingYear}";
                dbContext.Update(customer);
                dbContext.SaveChanges();
                return $"Tariff not found for customer {customer.CustomerName}.";
            }


            int? amount1 = unitDifference1 * Convert.ToInt32(tariff.Rate1); //9686
           




            // FPA LOGIC HERE 
            var fpaBills = dbContext.ElectricityBills
                .Where(b => b.Btno == customer.Btno &&
                       b.BillingMonth == FPAMONTH1 && b.BillingYear == FPAYEAR1)
                .ToList();
           
            var fpabill1 = fpaBills.FirstOrDefault(b => b.BillingMonth == FPAMONTH1 && b.BillingYear == FPAYEAR1);
            
            // Handle not found cases
            if (fpabill1 == null)
            {
                string missingMonths = "";

                if (fpabill1 == null)
                    missingMonths += $"FPA Month 1: {FPAMONTH1} ";

                customer.BillStatus = $"Bill for FPA not found for: {missingMonths}";
                dbContext.Update(customer);
                dbContext.SaveChanges();
                return $"Bill for FPA not found for: {missingMonths}";
            }

            // Calculation for FPA 1
            double FPACHAR1 = 0;
            double FPACHAR2 = 0;
            if (fpabill1.TotalUnit > 50)
            {
                double one1 = Math.Round(Convert.ToDouble(fpabill1.TotalUnit) * Convert.ToDouble(FPARATE1), 0);   
                double two1 = Math.Round((one1 * 1.5) / 100, 0);            
                double three1 = Math.Round((one1 + two1) * 0.18, 0);       
                FPACHAR1 = one1 + two1 + three1;                           
            }

            if (TotalUNits>50)
            {

            
            // Calculation for FPA 2 for current month 
          
            double one2 = Math.Round(Convert.ToDouble(unitDifference1) * Convert.ToDouble(FPARATE2), 0);   
            double two2 = Math.Round((one2 * 1.5) / 100, 0);                                               
            double three2 = Math.Round((one2 + two2) * 0.18, 0);                                           
            FPACHAR2 = one2 + two2 + three2;                                                               
            }

            // Final combined FPA charge
            double TotalFPACHAR = FPACHAR1 + FPACHAR2;



            //******************* FPA END HERE *****************// 

            //OPC  
            double? opc = GetTaxAmount("OPC", tariff.TarrifType);//9
            opc = Convert.ToDouble(unitDifference1 * opc);//1503


            //PTV FEES
            double? ptvFee = GetTaxAmount("PTVFee", customer.PlotType);


            //FURTHERTAX
            var furtherTax = GetTaxAmount("Further Tax", customer.PlotType);




            //GST & ENERGY COAST
            var gst = GetTaxAmount("GST", customer.PlotType);
            double? EnergyCoast = amount1 ;
            
            double  calcgst = Convert.ToInt32(Convert.ToInt32(EnergyCoast) - (Convert.ToInt32(EnergyCoast) / 1.18));
            EnergyCoast = EnergyCoast - calcgst ;
            decimal? BillAmountt = TotalUNits * tariff.Rate1;











            // BillCost
            // Calculate BillCost by adding values together, then rounding to the nearest whole number
            double? BillCost = Math.Round(Convert.ToDouble(EnergyCoast + opc + ptvFee + calcgst + TotalFPACHAR ), 0);

            // Calculate surcharge as 10% of EnergyCoast and round that as well
            double surcharge = Math.Round(Convert.ToDouble(EnergyCoast) * 10 / 100, 0);


            int RoundToNearestTen(decimal value)
            {
                int intValue = (int)Math.Floor(value);
                int remainder = intValue % 10;
                if (remainder >= 5)
                    return intValue + (10 - remainder); // round up
                else
                    return intValue - remainder;        // round down
            }


            // Create the new bill
            var newBill = new ElectricityBill
            {
                CustomerNo = customer.CustomerNo,
                Btno = customer.Btno,
                BillingMonth = currentBillingMonth,
                BillingYear = currentBillingYear,
                EnergyCoast =Convert.ToInt32(EnergyCoast),
                CurrentBill = (int?)(Math.Round(Convert.ToDecimal(BillCost))),
                BillAmountInDueDate = RoundToNearestTen(Convert.ToDecimal(BillCost) + Convert.ToDecimal(arrears)),
                BillSurcharge = (int?)surcharge,
                BillAmountAfterDueDate = RoundToNearestTen(Convert.ToDecimal(BillCost) + Convert.ToDecimal(surcharge) + Convert.ToDecimal(arrears)),

                CustomerName = customer.CustomerName,
                IssueDate = IssueDate,
                DueDate = DueDate,
                ValidDate = ValidDate,
                Opc = Convert.ToDecimal( opc),
                Ptvfee = Convert.ToDecimal(ptvFee),
                Furthertax = Convert.ToDecimal(furtherTax),
                Gst = Convert.ToDecimal(calcgst),
                PreviousReading1 = reading.Previous1,
                CurrentReading1 = reading.Present1,
                PreviousReading2 = reading.Previous2,
                CurrentReading2 = reading.Present2,
                PreviousSolarReading = reading.Previous3,
                CurrentSolarReading = reading.Present3,
                Difference1 = unitDifference1,
              
                TotalUnit = TotalUNits,
                BillAmount= BillAmountt,
                Arrears = Convert.ToDecimal(arrears),
                UpdateBy= UserName,
                UpdateOn=DateTime.Now.ToString("dd MMM yyyy"),
                FPACHARGES=Convert.ToDecimal(TotalFPACHAR),
                AmountPaid=0,
            };

            dbContext.ElectricityBills.Add(newBill);
            dbContext.SaveChanges();


            string month = DateTime.ParseExact(currentBillingMonth, "MMMM", null).ToString("MM");
            string year = DateTime.ParseExact(currentBillingYear, "yyyy", null).ToString("yyyy");

            //string paddedUid = newBill.Uid.ToString().PadLeft(8, '0');
            newBill.InvoiceNo = $"{year}{month}{newBill.CustomerNo.PadLeft(5, '0').Substring(Math.Max(0, newBill.CustomerNo.Length - 5))}";

            dbContext.Update(newBill);

            customer.BillStatus = $"Bill Generated for {currentBillingMonth} {currentBillingYear}";
            dbContext.Update(customer);
            dbContext.SaveChanges();

            return $"Bill created successfully for customer {customer.CustomerName}.";
        }






        private double ?GetTaxAmount(string taxName, string TarrifType)
        {
            return dbContext.TaxInformations
                .Where(t => t.TaxName == taxName && (t.ApplicableFor == TarrifType || t.ApplicableFor == "All"))
                .Sum(t => (double)t.TaxRate);
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

    }
}
