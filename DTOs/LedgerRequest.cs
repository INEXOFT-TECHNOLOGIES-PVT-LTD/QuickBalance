namespace QuickBalance.API.DTOs
{
    public class LedgerRequest
    {
        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public bool DetailReport { get; set; }

        public bool MultiCurrSelected { get; set; }

        public int MultiCurrCode { get; set; }

        public bool AllAccountSelected { get; set; }

        public bool MultiAccountSelected { get; set; }

        public string MultiAccountCode { get; set; } = string.Empty;

        public bool SingleAccountSelected { get; set; }

        public int SingleAccountCode { get; set; }

        public bool GroupSelected { get; set; }

        public int GroupCode { get; set; }

        public bool VoucherSummary { get; set; }

        public bool WithReference { get; set; }

        public bool IncludeUnapproved { get; set; }

        public string VoucherTypeCodes { get; set; } = string.Empty;
    }
}