namespace QuickBalance.API.DTOs.Brand
{

    public sealed class BrandResponse
    {
        public int Id { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public string? SecondName { get; set; }
        public string? Alias { get; set; }
    }
}
