// ViewModels/MaintenanceBillViewModel.cs
namespace BMSBT.ViewModels;

public class MaintenanceBillViewModel
{
    public string InvoiceNo { get; set; }
    public string CustomerName { get; set; }
    public string Btno { get; set; }
    public string BillingMonth { get; set; }
    public string BillingYear { get; set; }
    public int? BillAmountInDueDate { get; set; }
    public string PaymentStatus { get; set; }
    public string Block { get; set; } // From CustomersDetail
}