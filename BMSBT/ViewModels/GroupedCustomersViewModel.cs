using BMSBT.Models;

namespace BMSBT.ViewModels
{
    public class GroupedCustomersViewModel
    {
        public string SubProject { get; set; }
        public string Sector { get; set; }
        public List<CustomersDetail> Customers { get; set; }
    }
}
