using System.ComponentModel.DataAnnotations;

namespace BMSBT.Models
{

    public class Fine
    {
        public int FineID { get; set; }

        // Customer Info
        [Required(ErrorMessage = "BT No is required")]
        public string BTNo { get; set; }

        [Required(ErrorMessage = "Project Name is required")]
        public string ProjectName { get; set; }

        [Required(ErrorMessage = "Customer Name is required")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "Block is required")]
        public string Block { get; set; }

        [Required(ErrorMessage = "Sector is required")]
        public string Sector { get; set; }

        // Fine Details
        [Required(ErrorMessage = "Fine Month is required")]
        public string FineMonth { get; set; }

        [Required(ErrorMessage = "Fine Year is required")]
        public int FineYear { get; set; }

        [Required(ErrorMessage = "Fine Date is required")]
        [DataType(DataType.Date)]
        public DateTime DateFine { get; set; }

        [Required(ErrorMessage = "Department is required")]
        public string Department { get; set; }

        [Required(ErrorMessage = "Fine Type is required")]
        public string FineType { get; set; }

        public string? FineCategory { get; set; }

        [Required(ErrorMessage = "Fine Service is required")]
        public string FineService { get; set; }


        [Required(ErrorMessage = "Fine Amount is required")]
        public decimal? FineAmount { get; set; }

        [Required(ErrorMessage = "Waived Amount is required")]
        public decimal? WaivedAmount { get; set; }
        
        [Required(ErrorMessage = "Adjustment Amount is required")]
        public decimal? AdjustmentAmount { get; set; }

        [Required(ErrorMessage = "Fine To Charge is required")]
        public decimal FineToCharge { get; set; }

        public string? Comments { get; set; }
        public string? FineEnteredBy { get; set; }

        [Required(ErrorMessage = "Fine Enter Date is required")]
        [DataType(DataType.Date)]
        public DateTime FineEnterDate { get; set; }

        public string? History { get; set; }
    }




    //public class Fine
    //{
    //    public int FineID { get; set; }

    //    // Customer Info
    //    [Required]
    //    public string BTNo { get; set; }

    //    public string ProjectName { get; set; }
    //    public string CustomerName { get; set; }
    //    public string Block { get; set; }
    //    public string Sector { get; set; }

    //    // Fine Details
    //    public string FineMonth { get; set; }
    //    public int FineYear { get; set; }

    //    [DataType(DataType.Date)]
    //    public DateTime DateFine { get; set; }

    //    public string Department { get; set; }
    //    public string FineType { get; set; }
    //    public string FineCategory { get; set; }

    //    public int FineAmount { get; set; }
    //    public int WaivedAmount { get; set; }
    //    public int AdjustmentAmount { get; set; }
    //    public int FineToCharge { get; set; }

    //    public string Comments { get; set; }
    //    public string FineEnteredBy { get; set; }

    //    [DataType(DataType.Date)]
    //    public DateTime FineEnterDate { get; set; }

    //    public string History { get; set; }
    //}
}
