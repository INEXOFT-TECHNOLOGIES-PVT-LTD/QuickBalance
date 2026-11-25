namespace QuickBalance.API.DTOs
{
    public class ProductwisePurchaseReportRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public bool Purchase { get; set; }

        public string Type { get; set; } = "Product";

        public int ProductCode { get; set; }
        public int GroupCode { get; set; }

        public string? BrandCode { get; set; }

        public int CategoryCode { get; set; }
        public int GodownCode { get; set; }
        public int SupplierCode { get; set; }
        public int SupplierGroupCode { get; set; }

        public bool AllProduct { get; set; }
        public bool AllGroup { get; set; }
        public bool AllBrand { get; set; }
        public bool AllCategory { get; set; }
        public bool AllGodown { get; set; }
        public bool AllSupplier { get; set; }
        public bool AllSupplierGroup { get; set; }

        public bool IncludeUnapproved { get; set; }

        public string VoucherTypeCodes { get; set; } = string.Empty;

     
    }
}