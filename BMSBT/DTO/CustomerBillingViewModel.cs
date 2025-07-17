using BMSBT.Models;
using System.ComponentModel.DataAnnotations;

namespace BMSBT.DTO
{
    public class CustomerBillingViewModel
    {

        [Key]
        public int Uid { get; set; }
        public string CustomerNo { get; set; } = null!;
        public string? Btno { get; set; }
        public string? CustomerName { get; set; }
        public string? GeneratedMonthYear { get; set; }
        public string? LocationSeqNo { get; set; }
        public string? Cnicno { get; set; }
        public string? FatherName { get; set; }
        public string? InstalledOn { get; set; }
        public string? MobileNo { get; set; }
        public string? TelephoneNo { get; set; }
        public string? MeterType { get; set; }
        public string? Ntnnumber { get; set; }
        public string? City { get; set; }
        public string Project { get; set; } = null!;
        public string SubProject { get; set; } = null!;
        public string TariffName { get; set; } = null!;
        public string? BankNo { get; set; }
        public string? BtnoMaintenance { get; set; }
        public string Category { get; set; } = null!;
        public string Block { get; set; } = null!;
        public string? PlotType { get; set; }
        public string? Size { get; set; }
        public string Sector { get; set; } = null!;
        public string PloNo { get; set; } = null!;



        // Collection of multiple billing records
        public List<ElectricityBill> Bills { get; set; } = new List<ElectricityBill>();
        public List<MaintenanceBill> MBills { get; set; } = new List<MaintenanceBill>();
    }
}
