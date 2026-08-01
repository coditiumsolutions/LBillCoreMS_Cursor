using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BMSBT.Models;

/// <summary>
/// Testing / comparison copy of electricity bills (table: EBill_Comparison).
/// Production bills remain in ElectricityBills.
/// </summary>
[Table("EBill_Comparison")]
public partial class EBillComparison
{
    [Key]
    [Column("uid")]
    public int Uid { get; set; }

    public decimal? BillAmount { get; set; }

    public string? InvoiceNo { get; set; }

    public string? CustomerNo { get; set; }

    public string? CustomerName { get; set; }

    [Column("BTNo")]
    public string? Btno { get; set; }

    public string? BillingMonth { get; set; }

    public string? BillingYear { get; set; }

    public DateOnly? BillingDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateOnly? ReadingDate { get; set; }

    public DateOnly? IssueDate { get; set; }

    public DateOnly? ValidDate { get; set; }

    public string? MeterType { get; set; }

    public string? MeterNo { get; set; }

    public string? PaymentStatus { get; set; }

    public int? AmountPaid { get; set; }

    public DateOnly? PaymentDate { get; set; }

    public string? PaymentMethod { get; set; }

    public string? BankDetail { get; set; }

    public int? EnergyCoast { get; set; }

    public decimal? CurrentBill { get; set; }

    public string? LastUpdated { get; set; }

    public int? BillAmountInDueDate { get; set; }

    public int? BillSurcharge { get; set; }

    public int? BillAmountAfterDueDate { get; set; }

    public int? PreviousReading1 { get; set; }

    public int? CurrentReading1 { get; set; }

    public int? Difference1 { get; set; }

    public int? PreviousReading2 { get; set; }

    public int? CurrentReading2 { get; set; }

    public int? Difference2 { get; set; }

    public int? PreviousSolarReading { get; set; }

    public int? CurrentSolarReading { get; set; }

    public int? DifferenceSolar { get; set; }

    public decimal? TotalUnit { get; set; }

    [Column("OPC")]
    public decimal? Opc { get; set; }

    [Column("GST")]
    public decimal? Gst { get; set; }

    [Column("PTVFEE")]
    public decimal? Ptvfee { get; set; }

    [Column("FURTHERTAX")]
    public decimal? Furthertax { get; set; }

    public decimal? Arrears { get; set; }

    public string? History { get; set; }

    public string? CreatedBy { get; set; }

    public string? CreateOn { get; set; }

    public string? Block { get; set; }

    public string? Sector { get; set; }

    public decimal? FPACHARGES { get; set; }
}
