namespace BMSBT.EBillService
{
    public class ReadingDetails
    {
            public int? PreviousReading { get; set; }
            public int? CurrentReading { get; set; }
            public int? Difference { get; set; }
            public decimal? Amount { get; set; }
        
    }
}
