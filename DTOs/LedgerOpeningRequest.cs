namespace QuickBalance.API.DTOs
{
    public class LedgerOpeningRequest
    {
        public bool AccountWise { get; set; }

        public bool GroupWise { get; set; }

        public int AccountCode { get; set; }

        public int GroupCode { get; set; }

        public bool ShowZeroBalance { get; set; }

        public bool ShowParentGroup { get; set; }

        public bool FilterCostCentre { get; set; }

        public int CostCentreCode { get; set; }

        public DateTime ReportDate { get; set; }

        public bool IncludeUnapproved { get; set; }

        public string VoucherTypeCodes { get; set; } = string.Empty;
    }
}