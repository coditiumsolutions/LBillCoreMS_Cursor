using BMSBT.Models;

namespace BMSBT.ViewModels
{
    public class MaintenanceBillSearch
    {
        public int Id { get; set; }
        public string BillingMonth { get; set; }
        public string BillingYear { get; set; }
        public string Btno { get; set; }
        public int CustomerId { get; set; }
        public CustomersDetail Customer { get; set; }

    }
}

