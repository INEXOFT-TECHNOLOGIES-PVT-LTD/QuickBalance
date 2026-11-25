namespace QuickBalance.API.DTOs
{
    public class ProfitAndLossRequest
    {
        public string? ReportType { get; set; }
        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public bool DetailedReport { get; set; }

        public bool IncludeUnapproved { get; set; }

        public string VoucherTypeCodes { get; set; } = string.Empty;
    }
}
