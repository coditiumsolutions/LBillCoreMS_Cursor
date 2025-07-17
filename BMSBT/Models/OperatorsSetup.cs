using System;
using System.ComponentModel.DataAnnotations;

namespace BMSBT.Models
{
    public class OperatorsSetup
    {
        public int Uid { get; set; }

        public string OperatorID { get; set; } = null!;
        public string? OperatorName { get; set; }
        public string? BillingMonth { get; set; }
        public string? BillingYear { get; set; }
        public string? BankName { get; set; }
        public DateTime? ReadingDate { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public DateTime? ValidDate { get; set; }
        public string? History { get; set; }
        public string? FPAMonth1 { get; set; }
        public string? FPAYEAR1 { get; set; }
        public decimal? FPARate1 { get; set; }
        public string? FPAMonth2 { get; set; }
        public string? FPAYEAR2 { get; set; }
        public decimal? FPARate2 { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }

    }
}
