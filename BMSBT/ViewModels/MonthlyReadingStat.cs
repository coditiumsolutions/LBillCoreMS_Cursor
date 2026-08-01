namespace BMSBT.ViewModels
{
    public class MonthlyReadingStat
    {
        public string Month { get; set; } = "";
        public string Year { get; set; } = "";
        public string Label => $"{Month} {Year}";
        public int TotalEntered { get; set; }
        public int Pending { get; set; }
    }
}
