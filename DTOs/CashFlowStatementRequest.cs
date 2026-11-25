namespace QuickBalance.API.DTOs
{
    public class CashFlowStatementRequest
    {
        public bool DateWise { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public int FromMonth { get; set; }

        public int ToMonth { get; set; }

        public bool IncludeUnapproved { get; set; }

        public string VoucherTypeCodes { get; set; } = string.Empty;
    }
}