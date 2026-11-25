namespace QuickBalance.API.DTOs
{
    public class FinancialYearResponse
    {
        public string DatabaseName { get; set; } = string.Empty;

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public string FinancialYear { get; set; } = string.Empty;
    }
}