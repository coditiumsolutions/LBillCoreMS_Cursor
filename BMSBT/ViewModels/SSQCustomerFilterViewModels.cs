using BMSBT.Models;
using X.PagedList;
using X.PagedList.Extensions;

namespace BMSBT.ViewModels
{
    public class SSQECustomerFilterViewModel
    {
        public List<string> Projects { get; set; } = new List<string>();
        public List<string> Blocks { get; set; } = new List<string>();
        public string? SelectedProject { get; set; }
        public string? SelectedBlock { get; set; }
        public string? SearchBtNo { get; set; }
        public int CurrentPage { get; set; } = 1;
        public IPagedList<CustomersDetail> Customers { get; set; } = new List<CustomersDetail>().ToPagedList(1, 20);
    }

    public class SSQMCustomerFilterViewModel
    {
        public List<string> Projects { get; set; } = new List<string>();
        public List<string> Sectors { get; set; } = new List<string>();
        public string? SelectedProject { get; set; }
        public string? SelectedSector { get; set; }
        public string? SearchBtNo { get; set; }
        public int CurrentPage { get; set; } = 1;
        public IPagedList<CustomersMaintenance> Customers { get; set; } = new List<CustomersMaintenance>().ToPagedList(1, 20);
    }
}
