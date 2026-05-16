namespace BMSBT.ViewModels;

public class MaintenanceBillingReportViewModel
{
    /// <summary>True after the user clicks Load Report; false on initial navigation.</summary>
    public bool HasResults { get; set; }

    public string BillingYear { get; set; } = "";
    public string? BillingMonth { get; set; }
    public string ScopeLabel { get; set; } = "";
    public List<string> YearOptions { get; set; } = new();
    public List<string> MonthOptions { get; set; } = new();

    public int TotalCustomers { get; set; }
    public int TotalBills { get; set; }
    public double TotalBilledAmount { get; set; }
    public double TotalCollected { get; set; }
    public double TotalOutstanding { get; set; }
    public double RecoveryPercent { get; set; }
    public int TotalUsers { get; set; }
    public int PaidBillsCount { get; set; }
    public int UnpaidBillsCount { get; set; }
}
