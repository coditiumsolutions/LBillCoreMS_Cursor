using System.Collections.Generic;

namespace BMSBT.Models
{
    public class ConfigurationListViewModel
    {
        public IEnumerable<Configuration> Configurations { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string SearchTerm { get; set; }
        public int TotalRecords { get; set; }
        public int PageSize { get; set; }
    }
}
