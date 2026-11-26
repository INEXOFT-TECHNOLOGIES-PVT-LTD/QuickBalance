namespace QuickBalance.API.DTOs.Product;

public class ProductResponse
{
    public int Id { get; set; }

    public string? PrName { get; set; }
    public string? Alias { get; set; }
    public string? PrintName { get; set; }
    public string? Description { get; set; }

    public string? UpcCode { get; set; }
    public string? SkuCode { get; set; }
    public string? BarCode { get; set; }

    public int? GroupId { get; set; }
    public int? CategoryId { get; set; }
    public string? BrandId { get; set; }

    public int? Unit { get; set; }
    public int? AltUnit { get; set; }
    public decimal? UnitConv { get; set; }
    public int? ConvType { get; set; }

    public decimal? OpnStock { get; set; }
    public decimal? OpnStkval { get; set; }

    public int? PurPriceType { get; set; }
    public decimal? BasePurPrice { get; set; }
    public decimal? AltPurPrice { get; set; }

    public int? SalesPriceType { get; set; }
    public decimal? BaseSalePrice { get; set; }
    public decimal? BaseMinPrice { get; set; }
    public decimal? AltSalePrice { get; set; }
    public decimal? AltMinPrice { get; set; }

    public int? CostMeth { get; set; }

    public int? TaxCatId { get; set; }
    public int? SaleAcId { get; set; }
    public int? PurAcId { get; set; }

    public decimal? MinQty { get; set; }
    public decimal? ReOrdQty { get; set; }
    public decimal? MaxQty { get; set; }

    public bool? ServFlg { get; set; }
    public int? SupplierId { get; set; }
    public bool? BatchFlg { get; set; }
    public bool? ManDateFlg { get; set; }
    public bool? ExpDateFlg { get; set; }
    public int? ExpDays { get; set; }
    public bool? PriceCatFlg { get; set; }

    public bool? Active { get; set; }
    public decimal? Stock { get; set; }

    public string? LogCd { get; set; }
    //public DateTime? UTime { get; set; }
    public string? SecName { get; set; }
    public int? PriceCatId { get; set; }

    public decimal? OpnStkRate { get; set; }
    public decimal? CurConvRate { get; set; }
    public int? CurrencyId { get; set; }

    //public string? ImgPath { get; set; }

    public bool? DelFlg { get; set; }
    public bool? PosFlg { get; set; }
    public string? PrNotes { get; set; }

    public decimal? Bmpl { get; set; }
    public decimal? Bmpq { get; set; }
    public decimal? Bmpw { get; set; }

    public decimal? BaseMrp { get; set; }
    public decimal? AltMrp { get; set; }

    public bool? SlNoTrack { get; set; }
    public string? BmEqn { get; set; }

    // Approval details
    //public string? Aprvlst { get; set; }
    //public int? AprvlAby { get; set; }
    //public DateTime? AprvlAdt { get; set; }
    //public int? AprvlBby { get; set; }
    //public DateTime? AprvlBdt { get; set; }
    //public int? AprvlRby { get; set; }
    //public DateTime? AprvlRdt { get; set; }
    //public int? AprvlJby { get; set; }
    //public DateTime? AprvlJdt { get; set; }

    // Image from PRIMG
    public string? Image { get; set; }

    // SQL Server ROWVERSION
    public byte[] RowVersion { get; set; } = [];

    public List<ProductAliasResponse> Aliases { get; set; } = [];

    public List<ProductPriceCategoryResponse> PriceCategories { get; set; } = [];
}

public sealed class ProductImageResponse
{
    public int PrCode { get; set; }
    public string? Image { get; set; }

    //public byte[] ImageBytes { get; set; } = [];
    //public string ContentType { get; set; } = "application/octet-stream";
    //public string ETag { get; set; } = "";
}

public class ProductAliasResponse
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? AlCode { get; set; }
    public string? AlName { get; set; }
    public decimal Qty { get; set; }
    public int UnitId { get; set; }
    public decimal SalesPrice { get; set; }
    public decimal PurPrice { get; set; }
    public int AlType { get; set; }
    public int InvUnitCode { get; set; }
    public bool? Active { get; set; }
}


public class ProductPriceCategoryResponse
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int CategoryId { get; set; }
    public string? CatName { get; set; }
    public string? DiscStruct { get; set; }
    public decimal? LevFrom { get; set; }
    public decimal? LevTo { get; set; }
    public decimal? DiscPerc { get; set; }
    public decimal? EffValue { get; set; }
    public decimal? AppPrice { get; set; }
}


public sealed class ProductImageResponses
{
    public int PrCode { get; set; }
    public byte[] ImageBytes { get; set; } = [];
    public string ContentType { get; set; } = "application/octet-stream";
}