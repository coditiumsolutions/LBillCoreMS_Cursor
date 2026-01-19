using BMSBT.DTO;
using BMSBT.Models;
using Microsoft.EntityFrameworkCore;

namespace BMSBT.Services;

/// <summary>
/// Isolated service responsible only for inserting records into MaintenanceBills.
/// Does not depend on MaintenanceNew controllers or views.
/// </summary>
public interface IMaintenanceBillInsertService
{
    Task<MaintenanceBill> CreateAsync(MaintenanceBillCreateDto dto, CancellationToken cancellationToken = default);
}

public class MaintenanceBillInsertService : IMaintenanceBillInsertService
{
    private readonly BmsbtContext _dbContext;

    // Constants for billing calculations
    private const decimal SURCHARGE_PERCENTAGE = 0.10m; // 10% surcharge

    public MaintenanceBillInsertService(BmsbtContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MaintenanceBill> CreateAsync(MaintenanceBillCreateDto dto, CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);

        // Lookup tariff based on Project, PlotType, Size
        var tariff = LookupTariff(dto.Project, dto.PlotType, dto.Size);
        
        // Use tariff values if found, otherwise use 0 as default
        decimal maintCharges = tariff != null ? (decimal)tariff.Charges : 0m;
        int taxAmount = tariff != null ? ParseTaxValue(tariff.Tax) : 0;

        // Carry forward arrears logic
        decimal arrears = 0;
        int fineToChargeSum = 0;
        int waterCharges = 0;
        int otherCharges = 0;

        if (!string.IsNullOrEmpty(dto.BillingMonth) && !string.IsNullOrEmpty(dto.BillingYear) && !string.IsNullOrEmpty(dto.BTNo))
        {
            var (prevMonth, prevYear) = GetPreviousMonthYear(dto.BillingMonth, dto.BillingYear);
            arrears = await GetArrearsAmountAsync(dto.BTNo, prevMonth, prevYear, cancellationToken);

            // Dynamic Fine logic: Sum FineToCharge from Fine table for matching BTNo, Month, and Year
            if (int.TryParse(dto.BillingYear, out int currentYearInt))
            {
                fineToChargeSum = await _dbContext.Fine
                    .Where(f => f.BTNo == dto.BTNo && 
                               f.FineMonth == dto.BillingMonth && 
                               f.FineYear == currentYearInt &&
                               f.FineService == "Maintenance")
                    .SumAsync(f => f.FineToCharge, cancellationToken);
            }

            // Fetch Additional Charges (Water & Other) from AdditionalCharges table
            waterCharges = await _dbContext.AdditionalCharges
                .Where(a => a.BTNo == dto.BTNo && 
                           a.ServiceType == "Maintenance" && 
                           a.ChargesName == "Water Charges")
                .Select(a => a.ChargesAmount)
                .FirstOrDefaultAsync(cancellationToken) ?? 0;

            otherCharges = await _dbContext.AdditionalCharges
                .Where(a => a.BTNo == dto.BTNo && 
                           a.ServiceType == "Maintenance" && 
                           a.ChargesName == "Other Charges")
                .Select(a => a.ChargesAmount)
                .FirstOrDefaultAsync(cancellationToken) ?? 0;
        }

        // Calculate billing amounts based on tariff values, arrears, fine, and additional charges
        var billingCalculations = CalculateBillingAmounts(maintCharges, taxAmount, arrears, (decimal)fineToChargeSum, (decimal)waterCharges, (decimal)otherCharges);

        var bill = new MaintenanceBill
        {
            // Customer mapping
            CustomerNo = dto.CustomerNo,
            CustomerName = dto.CustomerName,
            Btno = dto.BTNo,
            PlotStatus = dto.PlotStatus,
            MeterNo = dto.MeterNo,

            // Billing period (optional, can be null if caller doesn't provide)
            BillingMonth = dto.BillingMonth,
            BillingYear = dto.BillingYear,

            // Tariff-based values (dynamically calculated from MaintenanceTarrif table)
            MaintCharges = maintCharges,
            TaxAmount = taxAmount,

            // Billing calculations (dynamically calculated based on MaintCharges + TaxAmount)
            BillAmountInDueDate = billingCalculations.BillAmountInDueDate,
            BillSurcharge = billingCalculations.BillSurcharge,
            BillAmountAfterDueDate = billingCalculations.BillAmountAfterDueDate,
            Arrears = arrears,
            Fine = fineToChargeSum,
            OtherCharges = otherCharges,
            WaterCharges = waterCharges,

            // Dates
            // Prefer values provided by caller (e.g., from OperatorsSetup), else fallback to today
            BillingDate = dto.BillingDate ?? today,
            IssueDate = dto.IssueDate ?? today,
            DueDate = dto.DueDate ?? today,
            ValidDate = dto.ValidDate ?? today,

        // Payment status fields (per requirement: all new bills start as unpaid)
        PaymentStatus = "unpaid",
        PaymentDate = null, // PaymentDate should be null for new bills
        PaymentMethod = "NA", // PaymentMethod should be "NA" (not "N/A")
        BankDetail = "NA", // BankDetail should be "NA" (not "N/A")
        
        LastUpdated = now,

            // Invoice number - simple unique placeholder logic
            InvoiceNo = GenerateInvoiceNo(now, dto.CustomerNo)
        };

        _dbContext.MaintenanceBills.Add(bill);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return bill;
    }

    /// <summary>
    /// Looks up MaintenanceTarrif record matching Project, PlotType, and Size.
    /// Returns null if no match found.
    /// </summary>
    private MaintenanceTarrif? LookupTariff(string? project, string? plotType, string? size)
    {
        if (string.IsNullOrWhiteSpace(project) || 
            string.IsNullOrWhiteSpace(plotType) || 
            string.IsNullOrWhiteSpace(size))
        {
            return null; // Cannot lookup without all three attributes
        }

        return _dbContext.MaintenanceTarrifs
            .FirstOrDefault(t => 
                t.Project == project.Trim() &&
                t.PlotType == plotType.Trim() &&
                t.Size == size.Trim());
    }

    /// <summary>
    /// Calculates billing amounts based on maintenance charges, tax amount, arrears, fine, and additional charges.
    /// Per Requirement:
    /// BillAmountInDueDate = Charges + Tax + Arrears + Fine + Water + Other
    /// BillSurcharge = (Charges + Tax) * 10 / 100
    /// BillAmountAfterDueDate = BillAmountInDueDate + BillSurcharge
    /// </summary>
    private static BillingCalculations CalculateBillingAmounts(decimal maintCharges, int taxAmount, decimal arrears = 0, decimal fine = 0, decimal water = 0, decimal other = 0)
    {
        // Step 1: Calculate BillAmountInDueDate = Charges + Tax + Arrears + Fine + Water + Other
        decimal inDueDateDecimal = maintCharges + taxAmount + arrears + fine + water + other;
        int billAmountInDueDate = (int)Math.Round(inDueDateDecimal, MidpointRounding.AwayFromZero);

        // Step 2: Calculate Bill Surcharge = 10% of (Charges + Tax) -- Surcharge is usually on the base charges+tax
        decimal baseChargesAndTax = maintCharges + taxAmount;
        decimal surchargeDecimal = baseChargesAndTax * SURCHARGE_PERCENTAGE;
        int billSurcharge = (int)Math.Round(surchargeDecimal, MidpointRounding.AwayFromZero);

        // Step 3: Calculate BillAmountAfterDueDate = BillAmountInDueDate + BillSurcharge
        decimal totalAfterDue = (decimal)billAmountInDueDate + (decimal)billSurcharge;
        int billAmountAfterDueDate = (int)Math.Round(totalAfterDue, MidpointRounding.AwayFromZero);

        return new BillingCalculations
        {
            BillAmountInDueDate = billAmountInDueDate,
            BillSurcharge = billSurcharge,
            BillAmountAfterDueDate = billAmountAfterDueDate
        };
    }

    /// <summary>
    /// Fetches the arrears amount from the previous month's unpaid bill.
    /// </summary>
    private async Task<decimal> GetArrearsAmountAsync(string btNo, string prevMonth, string prevYear, CancellationToken cancellationToken)
    {
        var prevBill = await _dbContext.MaintenanceBills
            .Where(b => b.Btno == btNo && b.BillingMonth == prevMonth && b.BillingYear == prevYear)
            .OrderByDescending(b => b.Uid) // Get the latest bill for that month if multiple exist
            .FirstOrDefaultAsync(cancellationToken);

        if (prevBill != null && (string.IsNullOrEmpty(prevBill.PaymentStatus) || prevBill.PaymentStatus.Equals("unpaid", StringComparison.OrdinalIgnoreCase)))
        {
            return prevBill.BillAmountAfterDueDate ?? 0;
        }

        return 0m;
    }

    /// <summary>
    /// Helper to calculate the previous month and year.
    /// </summary>
    private (string month, string year) GetPreviousMonthYear(string currentMonth, string currentYear)
    {
        var months = new[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
        int monthIdx = Array.IndexOf(months, currentMonth);
        
        // If month not found in standard list, return current as fallback
        if (monthIdx == -1) return (currentMonth, currentYear);

        if (!int.TryParse(currentYear, out int year)) return (currentMonth, currentYear);

        int prevMonthIdx = monthIdx == 0 ? 11 : monthIdx - 1;
        int prevYear = monthIdx == 0 ? year - 1 : year;

        return (months[prevMonthIdx], prevYear.ToString());
    }

    /// <summary>
    /// Helper class to hold billing calculation results.
    /// </summary>
    private class BillingCalculations
    {
        public int BillAmountInDueDate { get; set; }
        public int BillSurcharge { get; set; }
        public int BillAmountAfterDueDate { get; set; }
    }

    /// <summary>
    /// Parses Tax string value to integer.
    /// Handles percentage strings (e.g., "15%" -> 15) or numeric strings.
    /// Returns default value of 0 if parsing fails.
    /// </summary>
    private static int ParseTaxValue(string? taxString)
    {
        if (string.IsNullOrWhiteSpace(taxString))
        {
            return 0; // Safe default
        }

        // Remove percentage sign if present
        var cleaned = taxString.Trim().TrimEnd('%').Trim();

        // Try parsing as integer
        if (int.TryParse(cleaned, out int taxValue))
        {
            return taxValue;
        }

        // Try parsing as decimal and converting to int
        if (decimal.TryParse(cleaned, out decimal taxDecimal))
        {
            return (int)Math.Round(taxDecimal);
        }

        // If all parsing fails, return safe default
        return 0;
    }

    private static string GenerateInvoiceNo(DateTime now, string? customerNo)
    {
        // Per Requirement: YYYYMM + Last 5 digits of CUSTOMERNO
        // Example: 202601 + 22306 = 20260122306
        var datePart = now.ToString("yyyyMM");
        var cust = string.IsNullOrWhiteSpace(customerNo) ? "00000" : customerNo.Trim();
        
        // Get last 5 digits of customerNo
        var lastFive = cust.Length >= 5 ? cust[^5..] : cust.PadLeft(5, '0');
        
        return $"{datePart}{lastFive}";
    }
}

