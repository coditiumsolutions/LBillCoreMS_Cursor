namespace BMSBT.DTO
{
    public class DashboardStatisticsResult
    {

        public int TotalBillsGenerated { get; set; }
        public int TotalBillsPaid { get; set; }
        public decimal TotalBillAmountGenerated { get; set; }
        public decimal TotalBillAmountCollected { get; set; }
    }
}
