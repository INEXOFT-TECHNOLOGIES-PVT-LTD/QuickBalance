namespace QuickBalance.API.DTOs.Purchase
{
    public class PurchaseInvoiceCreateResponse
    {
        /// <summary>PURM.VRNO (identity).</summary>    
        public int VrNo { get; set; }

        /// <summary>PURM.ORDNO (voucher number from FYDETAILS.PURVRNO).</summary>    
        public int OrdNo { get; set; }

        /// <summary>PURM.INVNOTEXT (formatted voucher number).</summary>    
        public string InvNoText { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }

        public List<PurchaseInvoiceTaxSummaryResponse> TaxSummary { get; set; } = new();
    }

    public class PurchaseInvoiceTaxSummaryResponse
    {
        public int TaxCode { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }
    }
}
