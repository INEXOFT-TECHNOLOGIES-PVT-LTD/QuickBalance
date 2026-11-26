
namespace QuickBalance.API.DTOs.PriceCategory;

public sealed class PriceCategoryResponse
{
    public int Id { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public IReadOnlyList<PriceCategoryLevelResponse> Levels { get; set; }
        = [];
}

public sealed class PriceCategoryLevelResponse
{
    public int Id { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string DiscountStructure { get; set; } = string.Empty;

    public decimal LevelFrom { get; set; }

    public decimal LevelTo { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal EffectiveValue { get; set; }

    public decimal AppliedPrice { get; set; }
}
