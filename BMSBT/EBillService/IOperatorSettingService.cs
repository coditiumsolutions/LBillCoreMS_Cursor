namespace BMSBT.EBillService
{
    public interface IOperatorSettingService
    {
        string BillingMonth { get; }
        string BillingYear { get; }
        DateOnly IssueDate { get; }
        DateOnly DueDate { get; }   

        void SetOperatorSetting(string month, string year, DateOnly issue, DateOnly due); // ✅ Add this method
    }

}
