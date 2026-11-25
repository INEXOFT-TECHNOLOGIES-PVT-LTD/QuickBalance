namespace QuickBalance.API.DTOs
{
    public class ReceiptAndPaymentRequest
    {
        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public bool Alphabetical { get; set; }

        public bool Scheduled { get; set; }

        public bool Detailed { get; set; }

        public bool IncludeUnapproved { get; set; }

        public string VoucherTypeCodes { get; set; } = string.Empty;
    }
}