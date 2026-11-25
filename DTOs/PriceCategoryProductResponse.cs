namespace QuickBalance.API.DTOs
{
    public class PriceCategoryProductResponse
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public int CategoryCode { get; set; }
        public string CategoryName { get; set; } = "";
        public string DiscountStructure { get; set; } = "";
        public decimal LevelFrom { get; set; }
        public decimal LevelTo { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal EffectiveValue { get; set; }
        public decimal AppliedPrice { get; set; }

    }
}