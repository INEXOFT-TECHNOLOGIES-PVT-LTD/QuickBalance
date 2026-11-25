namespace QuickBalance.API.CLS
{
    public class ClsPrAlias
    {
        public int Code { get; set; }

        public int PrCode { get; set; }

        public string AlCode { get; set; } = "";

        public string AlName { get; set; } = "";

        public decimal Qty { get; set; }

        public int UnitCode { get; set; }

        public decimal SalesPrice { get; set; }

        public decimal PurPrice { get; set; }

        public int LogCd { get; set; }

        public bool Active { get; set; }

        public int AlType { get; set; }

        public int InvUnitCode { get; set; }
    }
}