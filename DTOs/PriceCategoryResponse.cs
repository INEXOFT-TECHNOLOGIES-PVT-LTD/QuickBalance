
namespace QuickBalance.API.DTOs.PriceCategory;

public sealed class PriceCategoryResponse
{
    public int Id { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public IReadOnlyList<PriceCategoryLevelResponse> Levels { get; set; }
        = [];
}

/// <summary>Flat row for GET /api/v1/price-categories (list only).</summary>
public sealed class PriceCategoryListItemDto
{
    public int Id { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public int LevelCount { get; set; }
}

public sealed class PriceCategoryLevelResponse
{
    public int Id { get; set; }

    public string DiscountStructure { get; set; } = string.Empty;

    public decimal LevelFrom { get; set; }

    public decimal LevelTo { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal EffectiveValue { get; set; }

    public decimal AppliedPrice { get; set; }
}
