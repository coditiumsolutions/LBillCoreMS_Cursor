using System.ComponentModel.DataAnnotations;

namespace BMSBT.Models
{
    public class TwoMonthOutstandingBill
    {
        [Key]
        public string BTNo { get; set; }                 // nvarchar
        public string CustomerName { get; set; }         // nvarchar
        public string Project { get; set; }              // nvarchar
        public string Block { get; set; }                // nvarchar
        public string Sector { get; set; }               // nvarchar
        public string PloNo { get; set; }                // nvarchar

        public int MonthsOutstanding { get; set; }       // COUNT() = int
        public decimal TotalBill { get; set; }           // SUM(money) = decimal
        public decimal TotalPaid { get; set; }           // SUM(money) = decimal
        public decimal TotalOutstanding { get; set; }    // SUM(money) = decimal
    }
}
