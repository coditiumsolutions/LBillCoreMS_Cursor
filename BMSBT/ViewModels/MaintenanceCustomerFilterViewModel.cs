using System.Collections.Generic;
using System.Linq;
using BMSBT.Models;

namespace BMSBT.ViewModels
{
    public class MaintenanceCustomerFilterViewModel
    {
        public string? SelectedProject { get; set; }
        public string? SelectedBlock { get; set; }
        public string? SearchBtNo { get; set; }

        public List<string> Projects { get; set; } = new List<string>();
        public List<string> Blocks { get; set; } = new List<string>();

        public IEnumerable<CustomersMaintenance> Customers { get; set; }
            = Enumerable.Empty<CustomersMaintenance>();
    }
}

