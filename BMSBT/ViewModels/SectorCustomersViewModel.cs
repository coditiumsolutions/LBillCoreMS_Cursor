using BMSBT.Models;

namespace BMSBT.ViewModels
{
    public class SectorCustomersViewModel
    {
        public string Sector { get; set; }
        public List<CustomersDetail> Customers { get; set; }
    }





    public class MaintSectorCustomersViewModel
    {
        public string Sector { get; set; }
        public List<CustomersMaintenance> Customers { get; set; }
    }
}
