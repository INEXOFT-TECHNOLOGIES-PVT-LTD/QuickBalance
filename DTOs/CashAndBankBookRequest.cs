namespace QuickBalance.API.DTOs
{
    public class CashAndBankBookRequest
    {
        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public bool Summary { get; set; }

        public bool IncludeUnapproved { get; set; }

        public string VoucherTypeCodes { get; set; } = string.Empty;
    }
}
