namespace BMSBT.Requests
{
    public class MaintenanceBillRequest
    {
        public List<int> SelectedIds { get; set; }
        public string Month { get; set; }
        public string Project { get; set; }

        public string Year { get; set; }
    }






    public class ElectricityBillRequest
    {
        public List<int> SelectedIds { get; set; }
        public string Month { get; set; }
        public string Project { get; set; }

        public string Year { get; set; }
    }

}
