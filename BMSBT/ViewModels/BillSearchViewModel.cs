using System.Collections.Generic;
using BMSBT.Models;

namespace BMSBT.ViewModels
{
    public class BillSearchViewModel
    {
        public string SelectedMonth { get; set; }
        public string SelectedYear { get; set; }
        public string SelectedSector { get; set; }
        public string BtnoSearch { get; set; }

        public List<string> Months { get; set; } = new List<string>();
        public List<string> Years { get; set; } = new List<string>();
        public List<string> Sectors { get; set; } = new List<string>();

        public List<ElectricityBill> Results { get; set; } = new List<ElectricityBill>();
    }
}
