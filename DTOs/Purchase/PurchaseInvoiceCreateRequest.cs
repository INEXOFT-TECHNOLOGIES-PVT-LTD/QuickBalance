using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs.Purchase
{
    /// <summary>    
    /// Header of a purchase invoice. Totals are NOT accepted from the client:    
    /// the service recalculates every line and the invoice totals (review §12.1).    
    /// </summary>    
    public class PurchaseInvoiceCreateRequest
    {
        /// <summary>PURM purchase date (dtp_pur_date). Must fall inside the current financial year.</summary>    
        [Required]
        public DateTime PurchaseDate { get; set; }

        /// <summary>Supplier ledger (ACCMAS). "Invalid Supplier" when &lt; 1.</summary>    
        [Range(1, int.MaxValue, ErrorMessage = "Invalid Supplier")]
        public int SupplierCode { get; set; }

        /// <summary>Supplier bill number. Mandatory; unique per supplier.</summary>    
        [Required(ErrorMessage = "Missing invoice number.")]
        [MaxLength(100)]
        public string SupplierBillNo { get; set; } = string.Empty;

        /// <summary>Supplier bill date (dtp_inv_date). Defaults to PurchaseDate.</summary>    
        public DateTime? SupplierBillDate { get; set; }

        /// <summary>    
        /// Invoice format: 3 = tax inclusive, 4 = Not Registered, 5 = Exempt,    
        /// 6 = GCC Purchase, 7 = Exempt (outside party), 8 = Outside GCC; others = standard.    
        /// </summary>    
        [Range(1, int.MaxValue, ErrorMessage = "Invalid Invoice Format Selected")]
        public int InvoiceFormat { get; set; }

        /// <summary>1 = Cash (C), 2 = Credit (R), 3 = CD, 4 = Bank (B).</summary>    
        [Range(1, 4, ErrorMessage = "Please Choose Method of Payment")]
        public int PaymentMode { get; set; }

        /// <summary>Cash ledger for PaymentMode 1. Falls back to FYDETAILS.CASHCD.</summary>    
        public int CashLedgerCode { get; set; }

        /// <summary>Bank ledger for PaymentMode 4.</summary>    
        public int BankLedgerCode { get; set; }

        /// <summary>Cost centre (PURD.SLCODE). 0 = none.</summary>    
        public int CostCentreCode { get; set; }

        /// <summary>Currency master code. 0 = base currency.</summary>    
        public int CurrencyCode { get; set; }

        /// <summary>Conversion rate for a foreign currency.</summary>    
        public decimal ConversionRate { get; set; } = 1m;

        /// <summary>Additional cost spread over the lines (CalculateAdditionalCost).</summary>    
        [Range(0, double.MaxValue)]
        public decimal AdditionalCost { get; set; }

        public int AdditionalCostAccountCode { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        [MaxLength(500)]
        public string? ShipAddress { get; set; }

        /// <summary>    
        /// GRN tracking (chk_trck). Not supported in this version: must be false.    
        /// false means PURM.GRNNO = 0 and no GRN rows (review claim 11).    
        /// </summary>    
        public bool TrackingEnabled { get; set; }

        [MinLength(1, ErrorMessage = "Item Details Must not be blank")]
        public List<PurchaseInvoiceItemRequest> Items { get; set; } = new();

        /// <summary>    
        /// Rows of the legacy "additional info" grid, by index:    
        /// 0 freight, 1 discount (%), 2 customs duty, 3 VAT (ignored), 4 round-off, 5+ other amounts.    
        /// </summary>    
        public List<PurchaseInvoiceAdditionalInfoRequest> AdditionalInfo { get; set; } = new();

        /// <summary>    
        /// Optional. The tax summary the client shows on screen. When sent, the service    
        /// compares it with its own TaxSummary and rejects the save on a mismatch.    
        /// </summary>    
        public List<PurchaseInvoiceTaxDetailRequest>? ExpectedTaxDetails { get; set; }
    }
}
