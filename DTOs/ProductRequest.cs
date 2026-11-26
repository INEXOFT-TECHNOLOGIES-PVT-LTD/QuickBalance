using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QuickBalance.API.DTOs
{
    public class ProductRequest
    {
        public int PrCode { get; set; }

        public string PrName { get; set; } = "";
        public string Alias { get; set; } = "";
        public string PrintName { get; set; } = "";
        public string Desc { get; set; } = "";
        public string UpcCode { get; set; } = "";
        public string SkuCode { get; set; } = "";
        public string BarCode { get; set; } = "";
        public byte[]? RowVersion { get; set; }
        public int GroupCode { get; set; }
        public int CatgCode { get; set; }
        public string Brand { get; set; } = "";

        public int CompCode { get; set; }

        public int UserId { get; set; }

        public string VoucherTypeCodes { get; set; } = "";

        public int Unit { get; set; }
        public int AltUnit { get; set; }
        public decimal UnitConv { get; set; }
        public int ConvType { get; set; }

        public decimal OpnStock { get; set; }
        public decimal OpnStkval { get; set; }

        public int PurPriceType { get; set; }
        public decimal BasePurPrice { get; set; }
        public decimal AltPurPrice { get; set; }

        public int SalesPriceType { get; set; }
        public decimal BaseSalePrice { get; set; }
        public decimal BaseMinPrice { get; set; }
        public decimal AltSalePrice { get; set; }
        public decimal AltMinPrice { get; set; }

        public int CostMeth { get; set; }
        public int TaxCatCode { get; set; }

        public int SaleAcCode { get; set; }
        public int PurAcCode { get; set; }

        public decimal MinQty { get; set; }
        public decimal ReOrdQty { get; set; }
        public decimal MaxQty { get; set; }

        public bool ServFlg { get; set; }

        public int SupCode { get; set; }

        public bool BatchFlg { get; set; }
        public bool ManDateFlg { get; set; }
        public bool ExpDateFlg { get; set; }

        public decimal ExpDays { get; set; }

        public bool PriceCatFlg { get; set; }
        public bool Active { get; set; }

        public decimal Stock { get; set; }

        public string SecName { get; set; } = "";

        public int PriceCatCode { get; set; }

        public decimal OpnStkRate { get; set; }

        public decimal CurConvRate { get; set; }
        public int CurCode { get; set; }

        public string ImgPath { get; set; } = "";

        public bool PosFlg { get; set; }
        public bool DelFlg { get; set; }

        public string Notes { get; set; } = "";

        public decimal _BMPL { get; set; }
        public decimal _BMPQ { get; set; }
        public decimal _BMPW { get; set; }

        public decimal BaseMrp { get; set; }
        public decimal AltMrp { get; set; }

        public bool _SLNOTRACK { get; set; }

        public string _BMEQN { get; set; } = "";

        public string APRVLST { get; set; } = "";

        // =========================================================
        // DATABASE CONNECTION DETAILS
        // =========================================================

        public string ServerName { get; set; } = "";
        public string DatabaseName { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";

        // =========================================================
        // OPENING STOCK
        // =========================================================

        public int GdwCode { get; set; }

        public int BrCd { get; set; }

        public decimal OpnStkQty { get; set; }

        public decimal OpnStkValue { get; set; }

        // =========================================================
        // IMAGE
        // =========================================================

        public string? Image { get; set; }

        // =========================================================
        // ALIASES
        // =========================================================

        public List<PrAliasRequest> Aliases { get; set; }
            = new List<PrAliasRequest>();

        // =========================================================
        // PRICE CATEGORIES
        // =========================================================

        public List<PriceCategoryLevelRequest> PriceCategories { get; set; }
            = new();

        // =========================================================
        // MAP DTO TO CLS PRODUCT MASTER
        // =========================================================

        public QuickBalance.API.CLS.ClsPrMast ToClsPrMast()
        {
            return new QuickBalance.API.CLS.ClsPrMast
            {
                PrCode = PrCode,

                PrName = PrName,
                Alias = Alias,
                PrintName = PrintName,
                Desc = Desc,

                UpcCode = UpcCode,
                SkuCode = SkuCode,
                BarCode = BarCode,

                GroupCode = GroupCode,
                CatgCode = CatgCode,

                Brand = Brand,

                Unit = Unit,
                AltUnit = AltUnit,

                UnitConv = UnitConv,
                ConvType = ConvType,

                OpnStock = OpnStock,
                OpnStkval = OpnStkval,

                PurPriceType = PurPriceType,
                BasePurPrice = BasePurPrice,
                AltPurPrice = AltPurPrice,

                SalesPriceType = SalesPriceType,
                BaseSalePrice = BaseSalePrice,
                BaseMinPrice = BaseMinPrice,
                AltSalePrice = AltSalePrice,
                AltMinPrice = AltMinPrice,

                CostMeth = CostMeth,

                TaxCatCode = TaxCatCode,

                SaleAcCode = SaleAcCode,
                PurAcCode = PurAcCode,

                MinQty = MinQty,
                ReOrdQty = ReOrdQty,
                MaxQty = MaxQty,

                ServFlg = ServFlg,

                SupCode = SupCode,

                BatchFlg = BatchFlg,
                ManDateFlg = ManDateFlg,
                ExpDateFlg = ExpDateFlg,

                ExpDays = ExpDays,

                PriceCatFlg = PriceCatFlg,

                Active = Active,

                Stock = Stock,

                SecName = SecName,

                PriceCatCode = PriceCatCode,

                OpnStkRate = OpnStkRate,

                CurConvRate = CurConvRate,

                CurCode = CurCode,

                ImgPath = ImgPath,

                PosFlg = PosFlg,
                DelFlg = DelFlg,

                Notes = Notes,

                _BMPL = _BMPL,
                _BMPQ = _BMPQ,
                _BMPW = _BMPW,

                BaseMrp = BaseMrp,
                AltMrp = AltMrp,

                _SLNOTRACK = _SLNOTRACK,

                _BMEQN = _BMEQN,

                APRVLST = APRVLST
            };
        }
    }




    public class ProductRequests
    {
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string PrName { get; set; }

        [StringLength(50)]
        public string? Alias { get; set; }

        [StringLength(25)]
        public string? PrintName { get; set; }

        [StringLength(100)]
        public string? Desc { get; set; }

        [StringLength(150)]
        public string? UpcCode { get; set; }

        [StringLength(150)]
        public string? SkuCode { get; set; }

        [StringLength(150)]
        public string? BarCode { get; set; }

        [Range(1, int.MaxValue)]
        public int? GroupId { get; set; }

        [Range(1, int.MaxValue)]
        public int? CatgId { get; set; }

        [Range(1, int.MaxValue)]
        public int? BrandId { get; set; }

        [Range(1, int.MaxValue)]
        public int? Unit { get; set; }

        [Range(1, int.MaxValue)]
        public int? AltUnit { get; set; }

        [Range(typeof(decimal), "0.000001", "100000")]
        public decimal? UnitConv { get; set; }

        public int? ConvType { get; set; }

        //[Range(typeof(decimal), "0", "100000")]
        //public decimal? OpnStock { get; set; }

        //[Range(typeof(decimal), "0", "100000")]
        //public decimal? OpnStkval { get; set; }

        public int? PurPriceType { get; set; }

        [Range(typeof(decimal), "0", "100000")]
        public decimal? BasePurPrice { get; set; }

        [Range(typeof(decimal), "0", "100000")]
        public decimal? AltPurPrice { get; set; }

        public int? SalesPriceType { get; set; }

        [Range(typeof(decimal), "0", "100000")]
        public decimal? BaseSalePrice { get; set; }

        [Range(typeof(decimal), "0", "100000")]
        public decimal? BaseMinPrice { get; set; }

        [Range(typeof(decimal), "0", "100000")]
        public decimal? AltSalePrice { get; set; }

        [Range(typeof(decimal), "0", "100000")]
        public decimal? AltMinPrice { get; set; }

        public int? CostMeth { get; set; }

        [Range(1, int.MaxValue)]
        public int? TaxCatId { get; set; }

        [Range(1, int.MaxValue)]
        public int? SaleAccId { get; set; }

        [Range(1, int.MaxValue)]
        public int? PurAcctId { get; set; }

        [Range(typeof(decimal), "0", "100000")]
        public decimal? MinQty { get; set; }

        [Range(typeof(decimal), "0", "100000")]
        public decimal? ReOrdQty { get; set; }

        [Range(typeof(decimal), "0", "100000")]
        public decimal? MaxQty { get; set; }

        public bool? ServFlg { get; set; }

        [Range(1, int.MaxValue)]
        public int? SupplierId { get; set; }

        public bool? BatchFlg { get; set; }
        public bool? ManDateFlg { get; set; }
        public bool? ExpDateFlg { get; set; }

        [Range(0, 100000)]
        public int? ExpDays { get; set; }

        public bool? PriceCatFlg { get; set; }

        [Range(0, 100000)]
        public int? PriceCatId { get; set; }

        public bool? Active { get; set; }

        [Range(typeof(decimal), "0", "100000")]
        public decimal? Stock { get; set; }

        [StringLength(150)]
        public string? SecName { get; set; }

        [Range(typeof(decimal), "0", "100000")]
        public decimal? OpnStkRate { get; set; }

        [Range(typeof(decimal), "0", "100000")]
        public decimal? CurConvRate { get; set; }

        [Range(1, int.MaxValue)]
        public int? CurrencyId { get; set; }

        public bool? DelFlg { get; set; }
        public bool? PosFlg { get; set; }

        public string? Notes { get; set; }

        [JsonPropertyName("_BMPL")]
        [Range(typeof(decimal), "0", "100000")]
        public decimal? BMPL { get; set; }

        [JsonPropertyName("_BMPQ")]
        [Range(typeof(decimal), "0", "100000")]
        public decimal? BMPQ { get; set; }

        [JsonPropertyName("_BMPW")]
        [Range(typeof(decimal), "0", "100000")]
        public decimal? BMPW { get; set; }

        [Range(typeof(decimal), "0", "100000")]
        public decimal? BaseMrp { get; set; }

        [Range(typeof(decimal), "0", "100000")]
        public decimal? AltMrp { get; set; }

        [JsonPropertyName("_SLNOTRACK")]
        public bool? SLNOTRACK { get; set; }

        [JsonPropertyName("_BMEQN")]
        [StringLength(50)]
        public string? BMEQN { get; set; }

        //public int? APRVLABY { get; set; }
        //public DateTime? APRVLADT { get; set; }

        //public int? APRVLBBY { get; set; }
        //public DateTime? APRVLBDT { get; set; }

        [StringLength(250)]
        public string? APRVLST { get; set; }

        //public int? APRVLRBY { get; set; }
        //public DateTime? APRVLRDT { get; set; }

        //public int? APRVLJBY { get; set; }
        //public DateTime? APRVLJDT { get; set; }

        [Range(1, int.MaxValue)]
        public int? GdwnId { get; set; }

        [Range(1, int.MaxValue)]
        public int? BrCd { get; set; }

        [Range(typeof(decimal), "0", "100000")]
        public decimal? OpnStkQty { get; set; }

        [Range(typeof(decimal), "0", "10000000")]
        public decimal? OpnStkValue { get; set; }

        //public byte[]? RowVersion { get; set; }

        public List<PrAliasRequests> Aliases { get; set; } = [];
    }


    public class ProductUpdateRequests : ProductRequests
    {
        public byte[]? RowVersion { get; set; }
    }

    public sealed class PrAliasRequests
    {
        public int Id { get; set; }

        [StringLength(25)]
        public string? AlCode { get; set; }

        [StringLength(50)]
        public string? AlName { get; set; }

        [Range(typeof(decimal), "0", "100000")]
        public decimal? Qty { get; set; }

        [Range(1, int.MaxValue)]
        public int? UnitId { get; set; }

        [Range(typeof(decimal), "0", "100000")]
        public decimal? SalesPrice { get; set; }

        [Range(typeof(decimal), "0", "100000")]
        public decimal? PurPrice { get; set; }

        public int? AlType { get; set; }

        [Range(1, int.MaxValue)]
        public int? InvUnitCode { get; set; }

        public bool? Active { get; set; }
    }

    public sealed class ProductImageRequest
    {
        [Required(ErrorMessage = "Product ID is required.")]
        [Range(1, int.MaxValue)]

        public int ProductId { get; set; }

        [Required(ErrorMessage = "Product image is required.")]

        public IFormFile? ImageFile { get; set; }

    }

    public sealed class ProductFormRequest
    {
        [Required(ErrorMessage = "Product data is required.")]
        public string Product { get; set; } = "";     // product details as JSON text

        public IFormFile? ImageFile { get; set; }     // optional image file
    }
}


//    public sealed class ProductRequests
//    {
//        //public int PrCode { get; set; }

//        [Required]
//        [StringLength(100)]
//        public string PrName { get; set; } = string.Empty;

//        [StringLength(100)]
//        public string? Alias { get; set; }

//        [StringLength(100)]
//        public string? PrintName { get; set; }

//        [StringLength(255)]
//        public string? Desc { get; set; }

//        [StringLength(255)]
//        public string? UpcCode { get; set; }

//        [StringLength(255)]
//        public string? SkuCode { get; set; }

//        [StringLength(255)]
//        public string? BarCode { get; set; }

//        public int? GroupCode { get; set; }
//        public int? CatgCode { get; set; }

//        //[StringLength(255)]
//        public int? Brand { get; set; }

//        public int? Unit { get; set; }
//        public int? AltUnit { get; set; }

//        public decimal? UnitConv { get; set; }
//        public int? ConvType { get; set; }

//        public decimal? OpnStock { get; set; }
//        public decimal? OpnStkval { get; set; }

//        public int? PurPriceType { get; set; }
//        public decimal? BasePurPrice { get; set; }
//        public decimal? AltPurPrice { get; set; }

//        public int? SalesPriceType { get; set; }
//        public decimal? BaseSalePrice { get; set; }
//        public decimal? BaseMinPrice { get; set; }
//        public decimal? AltSalePrice { get; set; }
//        public decimal? AltMinPrice { get; set; }

//        public int? CostMeth { get; set; }

//        public int? TaxCatCode { get; set; }
//        public int? SaleAcCode { get; set; }
//        public int? PurAcCode { get; set; }

//        public decimal? MinQty { get; set; }
//        public decimal? ReOrdQty { get; set; }
//        public decimal? MaxQty { get; set; }

//        public bool? ServFlg { get; set; }

//        public int? SupCode { get; set; }

//        public bool? BatchFlg { get; set; }
//        public bool? ManDateFlg { get; set; }
//        public bool? ExpDateFlg { get; set; }

//        public int? ExpDays { get; set; }

//        public bool? PriceCatFlg { get; set; }

//        // Selected Price Category master ID (PRICECATMAS.CODE)
//        public int? PriceCatCode { get; set; }

//        public bool? Active { get; set; }

//        public decimal? Stock { get; set; }

//        [StringLength(1000)]
//        public string? SecName { get; set; }

//        public decimal? OpnStkRate { get; set; }
//        public decimal? CurConvRate { get; set; }

//        public int? CurCode { get; set; }

//        //public string ImgPath { get; set; } = string.Empty;
//        //public IFormFile? ImageFile { get; set; }

//        public bool? DelFlg { get; set; }
//        public bool? PosFlg { get; set; }

//        public string? Notes { get; set; }

//        [JsonPropertyName("_BMPL")]
//        public decimal? BMPL { get; set; }

//        [JsonPropertyName("_BMPQ")]
//        public decimal? BMPQ { get; set; }

//        [JsonPropertyName("_BMPW")]
//        public decimal? BMPW { get; set; }

//        public decimal? BaseMrp { get; set; }
//        public decimal? AltMrp { get; set; }

//        [JsonPropertyName("_SLNOTRACK")]
//        public bool? SLNOTRACK { get; set; }

//        [JsonPropertyName("_BMEQN")]
//        [StringLength(100)]
//        public string? BMEQN { get; set; }

//        // Approval details
//        public int? APRVLABY { get; set; }
//        public DateTime? APRVLADT { get; set; }

//        public int? APRVLBBY { get; set; }
//        public DateTime? APRVLBDT { get; set; }

//        [StringLength(250)]
//        public string? APRVLST { get; set; }

//        public int? APRVLRBY { get; set; }
//        public DateTime? APRVLRDT { get; set; }

//        public int? APRVLJBY { get; set; }
//        public DateTime? APRVLJDT { get; set; }

//        // Opening stock details
//        public int? GdwCode { get; set; }
//        public int? BrCd { get; set; }

//        public decimal? OpnStkQty { get; set; }
//        public decimal? OpnStkValue { get; set; }
//        public byte[]? RowVersion { get; set; }
//        // Product aliases
//        public List<PrAliasRequests> Aliases { get; set; } = [];
//    }

//    public sealed class PrAliasRequests
//    {
//        public int Code { get; set; }

//        public string? AlCode { get; set; }
//        public string? AlName { get; set; }

//        public decimal? Qty { get; set; }
//        public int? UnitCode { get; set; }

//        public decimal? SalesPrice { get; set; }
//        public decimal? PurPrice { get; set; }

//        public int? AlType { get; set; }
//        public int? InvUnitCode { get; set; }

//        public bool? Active { get; set; }
//    }


//}

//public sealed class ProductImageRequest
//{
//    public int PrCode { get; set; }
//    public IFormFile? ImageFile { get; set; }
