namespace QuickBalance.API.DTOs
{
    public class PurchaseOrderDTO
    {
        // Header
        public int OrdNo { get; set; }
        public int SupCode { get; set; }
        public string? VrType { get; set; }
        public DateTime? OrdDate { get; set; }

        public decimal DiscP { get; set; }
        public decimal DiscVal { get; set; }
        public decimal Tax { get; set; }
        public decimal NtValue { get; set; }
        public decimal TValue { get; set; }
        public decimal CoinAdj { get; set; }
        public decimal CusDuity { get; set; }
        public decimal Amount { get; set; }
        public decimal Freight { get; set; }

        public string? Remarks { get; set; }
        public string? ShipAddress { get; set; }

        public int InvNo { get; set; }
        public DateTime? InvDate { get; set; }

        public decimal CurConRate { get; set; }
        public int CurCode { get; set; }

        public int GrNo { get; set; }
        public bool CancelFlg { get; set; }

        public string? InvNoText { get; set; }
        public int VSVRTYPE { get; set; }
        public string? VSDOCNO { get; set; }
        public int VSORDCD { get; set; }

        public string? RefNo { get; set; }
        public string? Transferred { get; set; }

        public int InFormat { get; set; }

        // Print / custom fields
        public string? POF1 { get; set; }
        public string? POF2 { get; set; }
        public string? POF3 { get; set; }
        public string? POF4 { get; set; }
        public string? POF5 { get; set; }
        public string? POF6 { get; set; }
        public string? POF7 { get; set; }
        public string? POF8 { get; set; }
        public string? POF9 { get; set; }
        public string? POF10 { get; set; }
        public string? POF11 { get; set; }
        public string? POF12 { get; set; }
        public string? POF13 { get; set; }
        public string? POF14 { get; set; }
        public string? POF15 { get; set; }
        public string? POF16 { get; set; }
        public string? POF17 { get; set; }
        public string? POF18 { get; set; }
        public string? POF19 { get; set; }
        public string? POF20 { get; set; }

        public string? AprvlLst { get; set; }

        // Purchase Order Items
        public List<PurchaseOrderItemDTO> Items { get; set; } = new();

        // Additional / Tax details
        public List<PurchaseOrderAdditionalDTO> AdditionalDetails { get; set; } = new();

    }

    public class PurchaseOrderItemDTO
    {
        public int OrdNo { get; set; }
        public int SupCode { get; set; }
        public int PrCode { get; set; }
        public int UnitCode { get; set; }
        public int LogCd { get; set; }
        public int TaxCode { get; set; }

        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal Disc { get; set; }
        public decimal Tax { get; set; }

        public string? BType { get; set; }
        public DateTime? OrdDate { get; set; }
        public DateTime? CreatedDt { get; set; }

        public int GdwCode { get; set; }

        public decimal CurConRate { get; set; }
        public int CurCode { get; set; }

        public decimal OrgQty { get; set; }
        public decimal OrgRate { get; set; }

        public bool CancelFlg { get; set; }
        public bool PrFlg { get; set; }

        public int ConvType { get; set; }
        public decimal ConvFact { get; set; }
        public decimal ConvQty { get; set; }
        public decimal ConvRate { get; set; }

        public string? LDiscType { get; set; }
        public decimal LDiscPerc { get; set; }
        public decimal LDiscAmt { get; set; }
        public decimal DiscP { get; set; }

        public decimal BML { get; set; }
        public decimal BMQ { get; set; }
        public decimal BMW { get; set; }

        public int VSVRTYPE { get; set; }

        public string? PrDesc { get; set; }

        public int OrdRowId { get; set; }

        public string? AlCode { get; set; }
        public string? AlName { get; set; }
    }

   
}