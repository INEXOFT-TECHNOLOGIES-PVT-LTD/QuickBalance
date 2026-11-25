namespace QuickBalance.API.DTOs
{
    public class SupplierWiseProductReportRequest
    {
        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public bool Purchase { get; set; }

        public int ProductId { get; set; }

        public int GroupId { get; set; }

        public string? CompanyName { get; set; }

        public int CategoryId { get; set; }

        public int SupplierCode { get; set; }

        public int PartyGroup { get; set; }

        public bool Summary { get; set; }

        public bool LW { get; set; }

        public bool IncludeUnapproved { get; set; }

        public string VoucherTypeCodes { get; set; } = string.Empty;

     
    }
}