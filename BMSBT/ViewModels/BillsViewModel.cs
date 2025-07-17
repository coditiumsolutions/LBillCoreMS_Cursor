using BMSBT.Models;

namespace BMSBT.ViewModels
{
    public class BillsViewModel
    {
        public string Sector { get; set; }
        public string BillingYear { get; set; }
        public List<ElectricityBill> Bills { get; set; }
    }
}
