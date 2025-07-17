namespace BMSBT.EBillService
{
    public class OperatorSettingService : IOperatorSettingService
    {

        public string? BillingMonth { get; set; }
        public string? BillingYear { get; set; }
        public DateOnly IssueDate { get; set; }
        public DateOnly DueDate { get; set; }


        public void SetOperatorSetting(string month, string year, DateOnly issue, DateOnly due)
        {
            BillingMonth = month;
            BillingYear = year;
            IssueDate = issue;
            DueDate = due;  
        }

    }
}
