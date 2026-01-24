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

        // Database column is int, but we want to use decimal in the application
        // Use int? to match database, then convert when needed
        public int? ChargesAmountInt { get; set; }

        // Computed property to convert int to decimal
        [NotMapped]
        public decimal? ChargesAmount 
        { 
            get => ChargesAmountInt.HasValue ? (decimal?)ChargesAmountInt.Value : null;
            set => ChargesAmountInt = value.HasValue ? (int?)value.Value : null;
        }
    }
}

