using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BMSBT.Models
{
    [Table("Adjustments")]
    public class Adjustment
    {
        [Key]
        [Column("AdjustmentId")]
        public int AdjustmentId { get; set; }

        [Required]
        [StringLength(50)]
        public string BTNo { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string BillingType { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string AdjustmentName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string AdjustmentType { get; set; } = string.Empty;

        [Required]
        public int AdjustmentValue { get; set; }
    }
}
