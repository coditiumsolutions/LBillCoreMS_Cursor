namespace BMSBT.DTO;

public class BillLookupResponse
{
    public bool Found { get; set; }
    public string? Message { get; set; }
    public int? Uid { get; set; }
    public string? Btno { get; set; }
    public string? CustomerName { get; set; }
    public string? BillingMonth { get; set; }
    public string? BillingYear { get; set; }
    public int? Amount { get; set; }
    public string? PaymentStatus { get; set; }
    public string? ReferenceNumber { get; set; }
}

public class BillStatusUpdateResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? PaymentStatus { get; set; }
}
