namespace BMSBT.EBillService
{
    public interface IOperatorService
    {
        string BillingMonth { get; }
        string BillingYear { get; }
        string OperatorId { get; }
        string ReadingDate { get; }
        string DueDate { get; }

    }
}
