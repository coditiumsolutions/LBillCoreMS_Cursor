using BMSBT.Models;

namespace BMSBT.ViewModels
{
    public class MaintenanceOperationsViewModel
    {
        public string? BillingMonth { get; set; }
        public string? BillingYear { get; set; }
        public string? Btno { get; set; }

        public MaintenanceBill? Bill { get; set; }
        public CustomersMaintenance? Customer { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
