using System;

namespace QuickBalance.API.CLS
{
    public class ClsOpnStk
    {
        public int Id { get; set; }

        public int PrCode { get; set; }

        public int GdwCode { get; set; }

        public int Uom { get; set; }

        public int CostcCd { get; set; }

        public int BrCd { get; set; }

        public decimal Rate { get; set; }

        public decimal StVal { get; set; }

        public decimal Qty { get; set; }

        public decimal ConvQty { get; set; }

        public decimal ConvValue { get; set; }

        public decimal Mrp { get; set; }

        public decimal SalRate { get; set; }

        public string Batch { get; set; } = "";

        public string Status { get; set; } = "";

        public DateTime MfgDate { get; set; }

        public DateTime Expiry { get; set; }

        public bool ExpChk { get; set; }

        public int SNTSLNO { get; set; }
    }
}