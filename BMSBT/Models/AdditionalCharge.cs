using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BMSBT.Models
{
    [Table("AdditionalCharges")]
    public class AdditionalCharge
    {
        [Key]
        [Column("uid")]
        public int Uid { get; set; }

        [MaxLength(50)]
        public string? BTNo { get; set; }

        [MaxLength(100)]
        public string? ServiceType { get; set; }

        [MaxLength(100)]
        public string? ChargesName { get; set; }

        public int? ChargesAmount { get; set; }
    }
}

