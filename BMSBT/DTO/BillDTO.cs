namespace BMSBT.DTO
{
    public class BillDTO
    {
        public int Uid { get; set; }

        public string CustomerNo { get; set; } = null!;

        public string? Btno { get; set; }

        public string? CustomerName { get; set; }

        public string? GeneratedMonthYear { get; set; }

        public string? LocationSeqNo { get; set; }

        public string? Cnicno { get; set; }

        public string? FatherName { get; set; }

        public string? InstalledOn { get; set; }

        public string? MobileNo { get; set; }

        public string? TelephoneNo { get; set; }

        public string? Ntnnumber { get; set; }

        public string? City { get; set; }

        public string Project { get; set; } = null!;

        public string SubProject { get; set; } = null!;

        public string TariffName { get; set; } = null!;

        public string? BankNo { get; set; }

        public string? BtnoMaintenance { get; set; }

        public string Category { get; set; } = null!;

        public string Block { get; set; } = null!;

        public string? PlotType { get; set; }

        public string? Size { get; set; }

        public string Sector { get; set; } = null!;

        public string PloNo { get; set; } = null!;

        public string? BillStatusMaint { get; set; }

        public string? BillStatus { get; set; }


        

        public string? InvoiceNo { get; set; }

        

        public string? BillingMonth { get; set; }

        public string? BillingYear { get; set; }

        public DateOnly? BillingDate { get; set; }

        public DateOnly? DueDate { get; set; }

        public DateOnly? IssueDate { get; set; }

        public DateOnly? ValidDate { get; set; }

        public string? PaymentStatus { get; set; }

        public DateOnly? PaymentDate { get; set; }

        public string? PaymentMethod { get; set; }

        public string? BankDetail { get; set; }

    

        public decimal? TaxAmount { get; set; }

        public decimal? BillAmountInDueDate { get; set; }

        public decimal? BillSurcharge { get; set; }

        public decimal? BillAmountAfterDueDate { get; set; }
    }
}
