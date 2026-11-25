namespace QuickBalance.API.DTOs
{
    public class PurchaseTaxReportRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public string InvType { get; set; } = "";
        public string TaxType { get; set; } = "";
        public decimal Tax { get; set; }

        public bool MultiTax { get; set; }
        public bool IncludeUnapproved { get; set; }

        public string VoucherTypeCodes { get; set; } = "";

    }
}