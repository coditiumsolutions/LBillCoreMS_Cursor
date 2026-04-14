using System.Collections.Generic;

namespace BMSBT.ViewModels
{
    public class MaintenanceSummaryReportViewModel
    {
        public List<string> Projects { get; set; } = new List<string>();
        public List<string> Blocks { get; set; } = new List<string>();

        public string? SelectedProject { get; set; }
        public string? SelectedBlock { get; set; }
        public string? SelectedYear { get; set; }
        public string? SelectedMonth { get; set; }

        public bool HasResults { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalBillsGenerated { get; set; }
        public int PaidBillsCount { get; set; }
        public int UnpaidBillsCount { get; set; }
    }
}
