using BMSBT.Models;
using X.PagedList;
using X.PagedList.Extensions;

namespace BMSBT.ViewModels
{
    public class EBillCustomerFilterViewModel
    {
        public List<string> Projects { get; set; } = new List<string>();
        public List<string> Blocks { get; set; } = new List<string>();
        public IPagedList<CustomersDetail> Customers { get; set; } = new List<CustomersDetail>().ToPagedList(1, 20);
    }
}
