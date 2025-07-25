// ViewModels/MaintenanceBillViewModel.cs
namespace BMSBT.ViewModels;

public class MaintenanceBillViewModel
{
    public int Uid { get; set; }    
    public string InvoiceNo { get; set; }
    public string CustomerName { get; set; }
    public string Btno { get; set; }
    public string BillingMonth { get; set; }
    public string BillingYear { get; set; }
    public int? BillAmountInDueDate { get; set; }
    public int? BillAmountAfterDueDate { get; set; }
    public string PaymentStatus { get; set; }
    public string Block { get; set; } // From CustomersDetail
    public DateOnly? DueDate { get; set; }
    public string? History { get; set; }
}