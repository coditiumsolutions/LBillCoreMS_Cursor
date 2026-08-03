namespace BMSBT.Requests
{
    public class ElectricitySingleBillPrintRequest
    {
        public List<ElectricitySingleBillPrintItem> Items { get; set; } = new();
    }

    public class ElectricitySingleBillPrintItem
    {
        public string? BtNo { get; set; }
        public string? Month { get; set; }
        public string? Year { get; set; }
    }
}
