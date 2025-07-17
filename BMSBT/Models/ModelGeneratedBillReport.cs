namespace BMSBT.Models
{
    public class ModedGenertedBillReport
    {
        public string SelectedMonth { get; set; }
        public string SelectedYear { get; set; }
        public List<GeneratedBill> GeneratedBills { get; set; } = new List<GeneratedBill>();

    }

    public class GeneratedBill
    {
        public string CustomerName { get; set; }
        public string CustomerID { get; set; }
        public string BillingMonth { get; set; }
        public string BillingYear { get; set; }
        public decimal BillAmount { get; set; }
        public string BillStatus { get; set; } // "Created" or "Pending"
    }

}
