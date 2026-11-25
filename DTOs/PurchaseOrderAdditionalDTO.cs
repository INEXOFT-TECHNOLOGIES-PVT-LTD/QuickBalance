namespace QuickBalance.API.DTOs
{
    public class PurchaseOrderAdditionalDTO
    {
        public int OrdNo { get; set; }
        public DateTime? OrdDate { get; set; }
        public int SupCode { get; set; }

        public string? EType { get; set; }
        public int Code { get; set; }

        public decimal Amount { get; set; }
        public decimal Tax { get; set; }

        public string? BType { get; set; }
        public int LogCd { get; set; }

        public decimal TaxAmt { get; set; }

        public string? VrType { get; set; }

        public decimal CurConRate { get; set; }
        public int CurCode { get; set; }

        public bool DFlag { get; set; }

        public decimal OrgAmt { get; set; }
        public decimal OrgTaxAmt { get; set; }

        public bool CancelFlg { get; set; }

        public int VSVRTYPE { get; set; }
    }
}
