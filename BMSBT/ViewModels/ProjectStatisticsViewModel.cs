// ViewModels/ProjectStatisticsViewModel.cs (updated)
namespace BMSBT.ViewModels
{
    public class ProjectStatisticsViewModel
    {
        public string ProjectName { get; set; }
        public int TotalCustomers { get; set; }
    }

    public class BlockStatisticsViewModel
    {
        public string BlockName { get; set; }
        public int TotalCustomers { get; set; }
    }

    public class DashboardViewModel
    {
        public List<string> Projects { get; set; }
        public List<string> Blocks { get; set; }
        public int TotalAllCustomers { get; set; }
        public List<ProjectStatisticsViewModel> ProjectStatistics { get; set; }
        public List<BlockStatisticsViewModel> BlockStatistics { get; set; }
    }

    public class NamedCountViewModel
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }

    public class CustomersDashboardViewModel
    {
        public string CustomerType { get; set; } = "E";
        public string CustomerTypeLabel => CustomerType == "M" ? "M-Customers" : "E-Customers";
        public string SourceTable => CustomerType == "M" ? "Customers Maintenance" : "Customer Details";
        public int TotalCustomers { get; set; }
        public int TotalProjects { get; set; }
        public int TotalBlocks { get; set; }
        public int TotalCategories { get; set; }
        public List<NamedCountViewModel> ByProject { get; set; } = new();
        public List<NamedCountViewModel> ByBlock { get; set; } = new();
        public List<NamedCountViewModel> ByCategory { get; set; } = new();
    }
}