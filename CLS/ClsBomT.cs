namespace QuickBalance.API.CLS
{
    public class ClsBomT
    {
        public int BomCode { get; set; }

        public int PrCode { get; set; }

        public decimal Qty { get; set; }

        public string Type { get; set; } = "";

        public int Unit { get; set; }
    }
}