namespace QuickBalance.API.CLS
{
    public class ClsPriceCatLev
    {
        public int Code { get; set; }

        public int LogCd { get; set; }

        public int PrCode { get; set; }

        public string CatName { get; set; } = "";

        public string DiscStruct { get; set; } = "";

        public decimal LevFrom { get; set; }

        public decimal LevTo { get; set; }

        public decimal DiscPerc { get; set; }

        public decimal EffValue { get; set; }

        public DateTime Utime { get; set; }

        public int CatCode { get; set; }

        public decimal AppPrice { get; set; }
    }
}