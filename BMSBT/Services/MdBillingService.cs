using BMSBT.Models;
using Microsoft.EntityFrameworkCore;

namespace BMSBT.Services;

public class MdBillResult
{
    public int CustomerUid { get; set; }
    public bool Generated { get; set; }
    public string Status { get; set; } = "";
    public string? DebugInfo { get; set; }
}

/// <summary>
/// Bill generation service that implements the full pipeline
/// documented in BillingLogic/ResidentialBillLogic.md (ported from IBM Notes).
/// Phases: VerifyProcess → PreviousBill → GenerateBill → CalculateBill
/// </summary>
public interface IMdBillingService
{
    Task<MdBillResult> GenerateBillForCustomerAsync(
        int customerUid,
        string billingMonth, string billingYear,
        DateOnly? issueDate, DateOnly? dueDate, DateOnly? validDate,
        CancellationToken ct = default);
}

public class MdBillingService : IMdBillingService
{
    private readonly BmsbtContext _db;

    private static readonly string[] Months =
    {
        "January","February","March","April","May","June",
        "July","August","September","October","November","December"
    };

    public MdBillingService(BmsbtContext db) => _db = db;

    public async Task<MdBillResult> GenerateBillForCustomerAsync(
        int customerUid,
        string billingMonth, string billingYear,
        DateOnly? issueDate, DateOnly? dueDate, DateOnly? validDate,
        CancellationToken ct = default)
    {
        var customer = await _db.CustomersMaintenance.FindAsync(new object[] { customerUid }, ct);
        if (customer == null)
            return Skipped(customerUid, "Customer not found");

        string btNo = customer.BTNo ?? "";

        var additionalChargeBtKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(btNo))
            additionalChargeBtKeys.Add(btNo.Trim());
        if (!string.IsNullOrWhiteSpace(customer.BTNo))
            additionalChargeBtKeys.Add(customer.BTNo.Trim());
        if (!string.IsNullOrWhiteSpace(customer.BTNoMaintenance))
            additionalChargeBtKeys.Add(customer.BTNoMaintenance.Trim());
        var additionalChargeKeyList = additionalChargeBtKeys.ToList();

        // ═══════════════════════════════════════════════════════════════
        // PHASE 2 – VERIFY PROCESS  (ResidentialBillLogic.md §2)
        // ═══════════════════════════════════════════════════════════════

        // Step 2.1 – Duplicate Bill Check
        bool duplicate = await _db.MaintenanceBills
            .AnyAsync(b => b.Btno == btNo
                        && b.BillingMonth == billingMonth
                        && b.BillingYear == billingYear, ct);
        // Duplicate-bill guard disabled for testing – allows regenerating
        //if (duplicate)
        //    return await UpdateAndSkip(customer, customerUid,
        //        $"Bill Already Generated-{billingYear}-{billingMonth}", ct);

        // ═══════════════════════════════════════════════════════════════
        // PHASE 3 – PREVIOUS BILL LOOKUP  (§3)
        // ═══════════════════════════════════════════════════════════════

        var (prevMonth, prevYear) = GetPreviousMonthYear(billingMonth, billingYear);

        // Step 3.2 – Look up previous bill
        var previousBill = await _db.MaintenanceBills
            .Where(b => b.Btno == btNo && b.BillingMonth == prevMonth && b.BillingYear == prevYear)
            .OrderByDescending(b => b.Uid)
            .FirstOrDefaultAsync(ct);

        if (previousBill == null)
        {
            bool anyBillExists = await _db.MaintenanceBills.AnyAsync(b => b.Btno == btNo, ct);
            if (anyBillExists)
                return await UpdateAndSkip(customer, customerUid, "previous bill not exist", ct);
            // No bills at all → new customer, proceed
        }
        else
        {
            // Step 3.3 – Validate previous bill amounts
            if (previousBill.BillAmountInDueDate == null || previousBill.BillAmountAfterDueDate == null)
                return await UpdateAndSkip(customer, customerUid, "wrong previous bill amount", ct);
        }

        // ═══════════════════════════════════════════════════════════════
        // PHASE 4 – GENERATE BILL  (§4)
        // ═══════════════════════════════════════════════════════════════

        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);

        // --- Step 4.6 – Rate & Service Tax ---
        decimal maintCharges = 0m;
        decimal taxAmount = 0m;

        var categoryKey = customer.Category ?? customer.PlotType;
        MaintenanceTarrif? tariff;
        if (BlockIndicatesApartment(customer.Block))
        {
            var projectKey = customer.Project?.Trim() ?? string.Empty;
            tariff = string.IsNullOrEmpty(projectKey)
                ? null
                : await _db.MaintenanceTarrifs
                    .Where(t => t.Project != null && t.Category != null)
                    .Where(t => t.Project!.Trim() == projectKey && t.Category!.Trim().ToLower() == "apartment")
                    .OrderBy(t => t.Uid)
                    .FirstOrDefaultAsync(ct);
        }
        else
        {
            tariff = await _db.MaintenanceTarrifs
                .FirstOrDefaultAsync(t => t.Project == customer.Project
                                       && t.Category == categoryKey
                                       && t.Size == customer.Size, ct);
        }

        if (tariff != null)
        {
            maintCharges = (decimal)tariff.Charges;
            taxAmount = ParseTaxValue(tariff.Tax);
        }
        else
        {
            customer.BillStatusMaint = BlockIndicatesApartment(customer.Block)
                ? $"Tariff not found for {customer.Project} Apartment (block indicates apartment)"
                : $"Tariff not found for {customer.Project} {categoryKey} {customer.Size}";
        }

        // --- Step 4.7 – Fine ---
        decimal fine = 0m;
        decimal adjustment = 0m;

        if (int.TryParse(billingYear, out int yearInt))
        {
            var fineRecords = await _db.Fine
                .Where(f => f.BTNo == btNo
                          && f.FineMonth == billingMonth
                          && f.FineYear == yearInt
                          && f.FineService == "Maintenance")
                .ToListAsync(ct);

            if (fineRecords.Count > 0)
            {
                var firstFine = fineRecords[0];
                if (string.Equals(firstFine.FineType, "Adjustment", StringComparison.OrdinalIgnoreCase))
                {
                    adjustment = firstFine.AdjustmentAmount ?? 0m;
                    fine = 0m;
                }
                else
                {
                    fine = fineRecords.Sum(f => f.FineToCharge);
                    if (fine < 0) fine = 0m;
                }
            }
        }

        // --- Step 4.9 / 4.13 – Water & Other (sum all AdditionalCharges rows, Maintenance, matching any customer BT number)
        decimal waterCharges = 0m;
        decimal otherCharges = 0m;
        if (additionalChargeKeyList.Count > 0)
        {
            var additionalRows = await _db.AdditionalCharges
                .Where(a => a.BTNo != null && additionalChargeKeyList.Contains(a.BTNo))
                .ToListAsync(ct);
            var maintenanceRows = additionalRows
                .Where(a => string.Equals(a.ServiceType?.Trim(), "Maintenance", StringComparison.OrdinalIgnoreCase))
                .ToList();
            waterCharges = maintenanceRows
                .Where(a => string.Equals(a.ChargesName?.Trim(), "Water Charges", StringComparison.OrdinalIgnoreCase))
                .Sum(a => a.ChargesAmount ?? 0m);
            otherCharges = maintenanceRows
                .Where(a => string.Equals(a.ChargesName?.Trim(), "Other Charges", StringComparison.OrdinalIgnoreCase))
                .Sum(a => a.ChargesAmount ?? 0m);
        }

        // --- Step 4.12 – Arrears ---
        var (arrears, arrearsDebug) = GetArrears(previousBill);

        // ═══════════════════════════════════════════════════════════════
        // PHASE 5 – CALCULATE BILL  (§5)
        // ═══════════════════════════════════════════════════════════════

        // Step 5.1 – CurrentBill = MaintCharges + Tax + Fine + Water + Other
        decimal currentBill = maintCharges + taxAmount + fine + waterCharges + otherCharges;

        // Step 5.2 – Round to nearest 10  (legacy: Round(currentBill, -1))
        currentBill = RoundToNearest10(currentBill);

        // Step 5.3 – Surcharge base (MaintCharges + Tax only)
        decimal amountSurcharge = maintCharges + taxAmount;

        // Step 5.4 – OverallBill (Amount In Due Date)
        decimal overallBill = Math.Round(currentBill + arrears, 0, MidpointRounding.AwayFromZero);
        overallBill -= adjustment;

        // Step 5.5 – Surcharge (10% of MaintCharges + Tax)
        decimal surcharge = 0m;
        if (amountSurcharge > 0 && overallBill >= 0)
            surcharge = Math.Round(amountSurcharge * 0.10m, 0, MidpointRounding.AwayFromZero);

        // Step 5.6 – LateBill (Amount After Due Date)
        decimal lateBill = overallBill + surcharge;

        // Step 4.10 – Invoice No
        string invoiceNo = GenerateInvoiceNo(now, customer.CustomerNo);

        // ═══ Create MaintenanceBill record ═══
        var bill = new MaintenanceBill
        {
            CustomerNo    = customer.CustomerNo,
            CustomerName  = customer.CustomerName,
            Btno          = btNo,
            PlotStatus    = customer.PlotType,
            MeterNo       = customer.MeterNo,

            BillingMonth  = billingMonth,
            BillingYear   = billingYear,
            BillingDate   = today,
            IssueDate     = issueDate ?? today,
            DueDate       = dueDate ?? today,
            ValidDate     = validDate ?? today,

            PaymentStatus = "unpaid",
            PaymentDate   = null,
            PaymentMethod = "NA",
            BankDetail    = "NA",
            LastUpdated   = now,

            MaintCharges           = (int)Math.Round(maintCharges, MidpointRounding.AwayFromZero),
            TaxAmount              = (int)Math.Round(taxAmount, MidpointRounding.AwayFromZero),
            Fine                   = (int)Math.Round(fine, MidpointRounding.AwayFromZero),
            WaterCharges           = (int)Math.Round(waterCharges, MidpointRounding.AwayFromZero),
            OtherCharges           = (int)Math.Round(otherCharges, MidpointRounding.AwayFromZero),
            Arrears                = (int)Math.Round(arrears, MidpointRounding.AwayFromZero),
            BillAmountInDueDate    = (int)Math.Round(overallBill, MidpointRounding.AwayFromZero),
            BillSurcharge          = (int)Math.Round(surcharge, MidpointRounding.AwayFromZero),
            BillAmountAfterDueDate = (int)Math.Round(lateBill, MidpointRounding.AwayFromZero),

            InvoiceNo = invoiceNo
        };

        _db.MaintenanceBills.Add(bill);

        // Update customer status (§4 post-steps)
        customer.BillGenerationStatus = $"{billingMonth}-{billingYear}";
        customer.BillStatusMaint = $"Generated {billingMonth} {billingYear}";

        await _db.SaveChangesAsync(ct);

        return new MdBillResult
        {
            CustomerUid = customerUid,
            Generated = true,
            Status = $"{billingMonth}-{billingYear}",
            DebugInfo = arrearsDebug
        };
    }

    // ───────── Helpers ─────────

    private static (decimal arrears, string debugInfo) GetArrears(MaintenanceBill? previousBill)
    {
        if (previousBill == null)
            return (0m, "No previous bill → Arrears=0");

        string status = (previousBill.PaymentStatus ?? "").Trim();

        if (status.Equals("paid", StringComparison.OrdinalIgnoreCase)
            || status.Equals("paid with surcharge", StringComparison.OrdinalIgnoreCase)
            || status.Equals("paidwithsurcharge", StringComparison.OrdinalIgnoreCase))
            return (0m, $"PrevBill status='{status}' → Arrears=0");

        if (string.IsNullOrWhiteSpace(status)
            || status.Equals("unpaid", StringComparison.OrdinalIgnoreCase))
        {
            decimal amt = (decimal)(previousBill.BillAmountAfterDueDate ?? 0);
            return (amt, $"PrevBill status='{status}' → Arrears=BillAmountAfterDueDate={amt}");
        }

        if (status.Equals("partially paid", StringComparison.OrdinalIgnoreCase)
            || status.Equals("paritally paid", StringComparison.OrdinalIgnoreCase))
        {
            var billed    = previousBill.BillAmountInDueDate ?? 0;
            var paid      = previousBill.PaymentAmount       ?? 0;
            var remaining = billed - paid;
            var arrears   = remaining < 0 ? 0m : (decimal)remaining;
            return (arrears,
                $"PrevBill status='{status}' | BillAmountInDueDate={billed} | PaymentAmount={paid} | Remaining={remaining} → Arrears={arrears}");
        }

        return (0m, $"PrevBill status='{status}' (unrecognised) → Arrears=0");
    }

    private static decimal RoundToNearest10(decimal value)
    {
        return Math.Round(value / 10m, 0, MidpointRounding.AwayFromZero) * 10m;
    }

    private static (string month, string year) GetPreviousMonthYear(string currentMonth, string currentYear)
    {
        int idx = Array.IndexOf(Months, currentMonth);
        if (idx == -1 || !int.TryParse(currentYear, out int year))
            return (currentMonth, currentYear);

        int prevIdx  = idx == 0 ? 11 : idx - 1;
        int prevYear = idx == 0 ? year - 1 : year;
        return (Months[prevIdx], prevYear.ToString());
    }

    private static bool BlockIndicatesApartment(string? block) =>
        !string.IsNullOrWhiteSpace(block)
        && block.Contains("apartment", StringComparison.OrdinalIgnoreCase);

    private static decimal ParseTaxValue(string? taxString)
    {
        if (string.IsNullOrWhiteSpace(taxString)) return 0m;
        string cleaned = taxString.Trim().TrimEnd('%').Trim();
        return decimal.TryParse(cleaned, out decimal val) ? val : 0m;
    }

    private static string GenerateInvoiceNo(DateTime now, string? customerNo)
    {
        string datePart = now.ToString("yyyyMM");
        string cust = string.IsNullOrWhiteSpace(customerNo) ? "00000" : customerNo.Trim();
        string lastFive = cust.Length >= 5 ? cust[^5..] : cust.PadLeft(5, '0');
        return $"{datePart}{lastFive}";
    }

    private async Task<MdBillResult> UpdateAndSkip(
        CustomersMaintenance customer, int uid, string status, CancellationToken ct)
    {
        customer.BillGenerationStatus = status;
        customer.BillStatusMaint = status;
        await _db.SaveChangesAsync(ct);
        return Skipped(uid, status);
    }

    private static MdBillResult Skipped(int uid, string status) =>
        new() { CustomerUid = uid, Generated = false, Status = status };
}
