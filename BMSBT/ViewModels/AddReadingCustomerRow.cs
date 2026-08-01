namespace BMSBT.ViewModels
{
    public class AddReadingCustomerRow
    {
        public int Uid { get; set; }
        public string? Btno { get; set; }
        public string? CustomerName { get; set; }
        public string? PloNo { get; set; }
        public string? Block { get; set; }
        public string? Sector { get; set; }
        public string? Category { get; set; }
        public bool HasReading { get; set; }
        public int? PreviousReading { get; set; }
        public int? CurrentReading { get; set; }
    }
}
