namespace QuickBalance.API.DTOs
{
    public class RegionResponse
    {
        public int CountryId { get; set; }
        public int Id { get; set; }
        public string? RegionName { get; set; }
        public string? RegionSecName { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
