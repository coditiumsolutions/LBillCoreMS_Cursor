using Microsoft.AspNetCore.Mvc.Rendering;

namespace BMSBT.Models
{
    public class BillReportViewModel
    {
        public string? SelectedMonth { get; set; }
        public string? SelectedYear { get; set; }
        public int TotalCustomers { get; set; }  // Total Unique Customers
        public int TotalBillsCreated { get; set; }  // Bills Created for Selected Month/Year
        public int PendingBills { get; set; }  // Customers without Bills for Selected Month/Year

        public List<SelectListItem> Months { get; set; } = new();
        public List<SelectListItem> Years { get; set; } = new();
    }
}
