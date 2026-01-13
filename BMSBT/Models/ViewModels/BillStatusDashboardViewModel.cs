using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BMSBT.Models.ViewModels
{
    /// <summary>
    /// ViewModel for bill status dashboard on /Graphs.
    /// </summary>
    public class BillStatusDashboardViewModel
    {
        public int TotalBills { get; set; }

        public int PaidCount { get; set; }
        public int PaidWithSurchargeCount { get; set; }
        public int UnpaidCount { get; set; }

        /// <summary>
        /// Labels to use in charts (e.g. ["Paid", "Paid with Surcharge", "Unpaid"])
        /// </summary>
        public List<string> Labels { get; set; } = new List<string>();

        /// <summary>
        /// Counts per status, aligned with Labels.
        /// </summary>
        public List<int> Counts { get; set; } = new List<int>();

        // Selected values for filtering
        public string? SelectedMonth { get; set; }
        public string? SelectedYear { get; set; }

        // Dropdown lists
        public List<SelectListItem> MonthList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> YearList { get; set; } = new List<SelectListItem>();
    }
}

