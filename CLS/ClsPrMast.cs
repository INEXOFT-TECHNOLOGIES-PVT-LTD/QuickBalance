namespace QuickBalance.API.CLS
{
    //public class ClsPrMast
    //{
    //    public int PrCode { get; set; }
    //}
    public class ClsPrMast
    {
        public int PrCode { get; set; }

        public string PrName { get; set; } = "";
        public string Alias { get; set; } = "";
        public string PrintName { get; set; } = "";
        public string Desc { get; set; } = "";

        public string UpcCode { get; set; } = "";
        public string SkuCode { get; set; } = "";
        public string BarCode { get; set; } = "";

        public int GroupCode { get; set; }
        public int CatgCode { get; set; }

        public string Brand { get; set; } = "";

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

        public int LogCd { get; set; }

        public DateTime Utime { get; set; }

        public string SecName { get; set; } = "";

        public int PriceCatCode { get; set; }

        public decimal OpnStkRate { get; set; }

        public decimal CurConvRate { get; set; }

        public int CurCode { get; set; }

        public string ImgPath { get; set; } = "";

        public bool DelFlg { get; set; }

        public bool PosFlg { get; set; }

        public string Notes { get; set; } = "";

        public decimal _BMPL { get; set; }
        public decimal _BMPQ { get; set; }
        public decimal _BMPW { get; set; }

        public decimal BaseMrp { get; set; }
        public decimal AltMrp { get; set; }

        public bool _SLNOTRACK { get; set; }

        public string _BMEQN { get; set; } = "";

        public string APRVLST { get; set; } = "";
    }
}
