namespace BMSBT.ViewModels
{
    public class CustomerSearchState
    {
        public string? Project { get; set; }
        public string? Sector { get; set; }
        public string? Block { get; set; }
        public string? BtNo { get; set; }
        public int Page { get; set; } = 1;

        public bool HasFilters =>
            !string.IsNullOrWhiteSpace(Project) ||
            !string.IsNullOrWhiteSpace(Sector) ||
            !string.IsNullOrWhiteSpace(Block) ||
            !string.IsNullOrWhiteSpace(BtNo);
    }
}
