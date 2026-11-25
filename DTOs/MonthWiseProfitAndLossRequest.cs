namespace QuickBalance.API.DTOs
{
    public class MonthWiseProfitAndLossRequest
    {
        public int FromMonth { get; set; }

        public int ToMonth { get; set; }

        public bool IncludeUnapproved { get; set; }

        public string VoucherTypeCodes { get; set; } = string.Empty;
    }
}
