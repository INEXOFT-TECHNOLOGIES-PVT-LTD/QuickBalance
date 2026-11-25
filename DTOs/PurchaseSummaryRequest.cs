namespace QuickBalance.API.DTOs
{
    public class PurchaseSummaryRequest
    {       
        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public bool RemoveGroupTotal { get; set; }

        public bool DateWise { get; set; }

        public string? PaymentType { get; set; }

        public int SupplierCode { get; set; }

        public string? VoucherType { get; set; }

        public int SupplierGroupCode { get; set; }

        public bool IncludeUnapproved { get; set; }

        public string VoucherTypeCodes { get; set; } = string.Empty;


    }
}