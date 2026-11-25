namespace QuickBalance.API.DTOs
{
    public class VoucherQueryRequest
    {
        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public int AccountCode { get; set; }

        public string VoucherNo { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string VoucherType { get; set; } = "All";

        public bool Exact { get; set; }

        public bool IncludeUnapproved { get; set; }

        public string VoucherTypeCodes { get; set; } = string.Empty;
    }
}