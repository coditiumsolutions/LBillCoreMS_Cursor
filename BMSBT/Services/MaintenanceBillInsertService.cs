using BMSBT.DTO;
using BMSBT.Models;

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
        
        // Use tariff values if found, otherwise use safe defaults
        decimal maintCharges = tariff != null ? (decimal)tariff.Charges : 15m;
        int taxAmount = tariff != null ? ParseTaxValue(tariff.Tax) : 15;

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

            // Defaults for other numeric fields (per requirement: set to 15 initially)
            BillAmountInDueDate = 15,
            BillSurcharge = 15,
            BillAmountAfterDueDate = 15,
            Arrears = 15m,
            Fine = 15,
            OtherCharges = 15,
            WaterCharges = 15,

            // Dates
            // Prefer values provided by caller (e.g., from OperatorsSetup), else fallback to today
            BillingDate = dto.BillingDate ?? today,
            IssueDate = dto.IssueDate ?? today,
            DueDate = dto.DueDate ?? today,
            ValidDate = dto.ValidDate ?? today,

            // Payment status fields (per requirement: all new bills start as Unpaid)
            PaymentStatus = "Unpaid",
            PaymentDate = null, // PaymentDate should be null for new bills
            PaymentMethod = "NA", // PaymentMethod should be "NA" (not "N/A")
            BankDetail = "NA", // BankDetail should be "NA" (not "N/A")
            
            LastUpdated = now.ToString("yyyy-MM-dd HH:mm:ss"),

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
    /// Parses Tax string value to integer.
    /// Handles percentage strings (e.g., "15%" -> 15) or numeric strings.
    /// Returns default value of 15 if parsing fails.
    /// </summary>
    private static int ParseTaxValue(string? taxString)
    {
        if (string.IsNullOrWhiteSpace(taxString))
        {
            return 15; // Safe default
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
        return 15;
    }

    private static string GenerateInvoiceNo(DateTime now, string? customerNo)
    {
        // Simple, unique-enough invoice pattern: MB-YYYYMM-<CustomerNo>-<ticks-part>
        var prefix = $"MB-{now:yyyyMM}";
        var cust = string.IsNullOrWhiteSpace(customerNo) ? "NA" : customerNo.Trim();
        var ticksPart = now.Ticks.ToString()[^5..]; // last 5 digits
        return $"{prefix}-{cust}-{ticksPart}";
    }
}

