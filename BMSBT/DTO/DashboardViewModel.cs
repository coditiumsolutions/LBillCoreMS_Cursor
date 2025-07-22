namespace BMSBT.DTO
{
    public class DashboardViewModel
    {
        public List<string> Projects { get; set; } = new List<string>();
        public List<string> Years { get; set; } = new List<string>();
        public List<string> Months { get; set; } = new List<string>();

        // Generated Detail
        public int TotalBillsGenerated { get; set; }
        public decimal TotalBillAmountGenerated { get; set; }
        public int BillsUnits { get; set; }

        // Net Metering Detail
        public int NetMeterBillsGenerated { get; set; }
        public decimal NetMeterTotalBilling { get; set; }
        public int NetMeterBillsUnits { get; set; }

        // Payments Detail
        public decimal TotalBillAmountCollected { get; set; }
        public int TotalBillsPaid { get; set; }
        public decimal BillUnpaidAmount { get; set; }
        public int UnpaidBillsCount { get; set; }

        // Raw data from SP
        public List<BillingReportData> BillingReportData { get; set; } = new List<BillingReportData>();
        public List<BillingData> BillingData { get; set; } = new List<BillingData>();
    }

    public class BillingData
    {
        public string Project { get; set; }
        public string SubProject { get; set; }
        public string Sector { get; set; }
        public string Block { get; set; }
        public string BillingMonth { get; set; }
        public int TotalBillsGenerated { get; set; }
        public int TotalBillsPaid { get; set; }
        public decimal TotalBillAmountGenerated { get; set; }
        public decimal TotalBillAmountCollected { get; set; }
    }
    public class BillingReportData
    {
        public string Section { get; set; }
        public string Metric { get; set; }
        public decimal Value { get; set; }
        public string Note { get; set; }
        public int? SecondaryValue { get; set; }
    }

}

