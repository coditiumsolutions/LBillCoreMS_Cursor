using BMSBT.Models;
using Microsoft.EntityFrameworkCore;

namespace BMSBT.Services;

/// <summary>
/// Central duplicate policy: a maintenance bill is considered duplicate when
/// one already exists for the same BT number (including alternate maintenance BT),
/// billing month, and billing year — allowing common month/year format differences.
/// </summary>
public static class MaintenanceBillDuplicateChecker
{
    private static readonly string[] MonthNames =
    {
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    };

    /// <summary>
    /// All BT numbers on the customer record that may appear on <see cref="MaintenanceBill.Btno"/>.
    /// </summary>
    public static IReadOnlyList<string> CollectCustomerBtKeys(CustomersMaintenance customer)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(customer.BTNo))
            set.Add(customer.BTNo.Trim());
        if (!string.IsNullOrWhiteSpace(customer.BTNoMaintenance))
            set.Add(customer.BTNoMaintenance.Trim());
        return set.ToList();
    }

    /// <summary>
    /// Month strings that should be treated as the same period (e.g. March, 3, 03).
    /// </summary>
    public static IReadOnlyList<string> MonthVariants(string? month)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(month))
            return set.ToList();

        var m = month.Trim();
        set.Add(m);

        if (int.TryParse(m, out int num) && num is >= 1 and <= 12)
        {
            set.Add(num.ToString());
            set.Add(num.ToString("D2"));
            set.Add(MonthNames[num - 1]);
            return set.ToList();
        }

        for (int i = 0; i < 12; i++)
        {
            if (MonthNames[i].Equals(m, StringComparison.OrdinalIgnoreCase))
            {
                set.Add(MonthNames[i]);
                set.Add((i + 1).ToString());
                set.Add((i + 1).ToString("D2"));
                break;
            }
        }

        return set.ToList();
    }

    public static IReadOnlyList<string> YearVariants(string? year)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(year))
            return set.ToList();

        var y = year.Trim();
        set.Add(y);
        if (int.TryParse(y, out int yi))
            set.Add(yi.ToString());
        return set.ToList();
    }

    /// <summary>
    /// Human-readable month for status text (e.g. March).
    /// </summary>
    public static string ToDisplayMonth(string? month)
    {
        if (string.IsNullOrWhiteSpace(month))
            return "";

        var m = month.Trim();
        if (int.TryParse(m, out int num) && num is >= 1 and <= 12)
            return MonthNames[num - 1];

        for (int i = 0; i < 12; i++)
        {
            if (MonthNames[i].Equals(m, StringComparison.OrdinalIgnoreCase))
                return MonthNames[i];
        }

        return m;
    }

    public static string BuildAlreadyGeneratedStatus(string? billingMonth, string? billingYear)
    {
        var displayMonth = ToDisplayMonth(billingMonth);
        var y = string.IsNullOrWhiteSpace(billingYear) ? "" : billingYear.Trim();
        return $"Bill Already Generated for {displayMonth} {y}".TrimEnd();
    }

    /// <summary>
    /// Case-insensitive BT match after trim (cannot be expressed in a single EF-translatable predicate).
    /// </summary>
    private static bool BtnoMatchesAnyKey(string? dbBtno, IReadOnlyList<string> btKeys)
    {
        if (string.IsNullOrWhiteSpace(dbBtno))
            return false;

        var trimmed = dbBtno.Trim();
        foreach (var k in btKeys)
        {
            if (string.IsNullOrWhiteSpace(k))
                continue;
            if (string.Equals(trimmed, k.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static bool BillExists(
        BmsbtContext db,
        IReadOnlyList<string> btKeys,
        string billingMonth,
        string billingYear)
    {
        if (btKeys == null || btKeys.Count == 0)
            return false;

        var months = MonthVariants(billingMonth);
        var years = YearVariants(billingYear);
        if (months.Count == 0 || years.Count == 0)
            return false;

        var monthSet = months.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var yearArray = years.ToArray();

        // Filter by year in SQL (translates to IN). Month + BT are matched in memory so EF does not
        // try to translate string.Equals(..., OrdinalIgnoreCase) or Trim on the server.
        var rows = db.MaintenanceBills.AsNoTracking()
            .Where(b => b.BillingYear != null && yearArray.Contains(b.BillingYear))
            .Select(b => new { b.Btno, b.BillingMonth })
            .ToList();

        return rows.Any(r =>
            r.BillingMonth != null &&
            monthSet.Contains(r.BillingMonth) &&
            BtnoMatchesAnyKey(r.Btno, btKeys));
    }

    public static async Task<bool> BillExistsAsync(
        BmsbtContext db,
        IReadOnlyList<string> btKeys,
        string billingMonth,
        string billingYear,
        CancellationToken cancellationToken = default)
    {
        if (btKeys == null || btKeys.Count == 0)
            return false;

        var months = MonthVariants(billingMonth);
        var years = YearVariants(billingYear);
        if (months.Count == 0 || years.Count == 0)
            return false;

        var monthSet = months.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var yearArray = years.ToArray();

        var rows = await db.MaintenanceBills.AsNoTracking()
            .Where(b => b.BillingYear != null && yearArray.Contains(b.BillingYear))
            .Select(b => new { b.Btno, b.BillingMonth })
            .ToListAsync(cancellationToken);

        return rows.Any(r =>
            r.BillingMonth != null &&
            monthSet.Contains(r.BillingMonth) &&
            BtnoMatchesAnyKey(r.Btno, btKeys));
    }
}
