namespace BMSBT.BillServices
{
    public static class BillCreationState
    {
        public static int MinBillResidential { get; set; }
        public static int MinBillCommercial { get; set; }
        public static int MinBillResidentialPlaza { get; set; }
        public static string? CurrentMonth { get; set; }
        public static string? CurrentYear { get; set; }
        public static string? PreviousMonth { get; set; }
        public static string? PreviousYear { get; set; }
        public static List<string> TempValues { get; set; } = new List<string>();
    }
}
