namespace QuickBalance.API.DTOs
{
    public class GroupwisePurchaseReportRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // true = Purchase, false = Purchase Return
        public bool Purchase { get; set; }

        public int GodownCode { get; set; }
        public int SupplierCode { get; set; }
        public int SupplierGroupCode { get; set; }

        public bool AllGodown { get; set; }
        public bool AllSupplier { get; set; }
        public bool AllSupplierGroup { get; set; }

        public bool IncludeUnapproved { get; set; }

        public string VoucherTypeCodes { get; set; } = string.Empty;

    
    }
}