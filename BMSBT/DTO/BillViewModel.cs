namespace BMSBT.DTO
{
    public class BillViewModel
    {
        public string? BillingMonth { get; set; }
        public string? BillingYear { get; set; }
        public string? MeteringType { get; set; }
        public DateTime? PaidOn { get; set; }
        public string? PaymentType { get; set; }
        public string? BankBranch { get; set; }
        public string? Btno { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Name { get; set; }
        public string? CustomerName { get; set; }
    }
}
