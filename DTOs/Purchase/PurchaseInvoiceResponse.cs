namespace QuickBalance.API.DTOs.Purchase
{
    public class PurchaseInvoiceResponse
    {
        public int VrNo { get; set; }
        public int OrdNo { get; set; }
        public string InvNoText { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public int SupplierCode { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierBillNo { get; set; } = string.Empty;
        public DateTime? SupplierBillDate { get; set; }
        public int InvoiceFormat { get; set; }
        public string PaymentType { get; set; } = string.Empty;   // BTYPE: C / R / CD / B    
        public int AdjustmentLedgerCode { get; set; }             // ADJCD    
        public int CurrencyCode { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal TotalAmount { get; set; }                  // NTVALUE    
        public decimal TaxableAmount { get; set; }                // TVALUE    
        public decimal TaxAmount { get; set; }                    // TAX    
        public decimal Freight { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal CustomsDuty { get; set; }
        public decimal RoundOff { get; set; }
        public decimal NetAmount { get; set; }                    // AMOUNT    
        public decimal AdditionalCost { get; set; }
        public string? Remarks { get; set; }
        public string? ShipAddress { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public List<PurchaseInvoiceItemResponse> Items { get; set; } = new();
        public List<PurchaseInvoiceDetailRowResponse> Details { get; set; } = new();
    }

    public class PurchaseInvoiceItemResponse
    {
        public int SlNo { get; set; }
        public int ProductCode { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int UnitCode { get; set; }
        public decimal Qty { get; set; }
        public decimal ConvQty { get; set; }
        public decimal Price { get; set; }                        // PRRATE    
        public decimal ConvRate { get; set; }
        public decimal Mrp { get; set; }
        public decimal LineDiscountPercentage { get; set; }
        public decimal LineDiscountAmount { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountAmount { get; set; }               // DISC    
        public decimal Amount { get; set; }
        public int TaxCode { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal TaxAmount { get; set; }
        public int GodownCode { get; set; }
        public string? Batch { get; set; }
        public DateTime? MfgDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal AddCost { get; set; }
    }

    /// <summary>PURD row: ETYPE T = tax summary, A = additional info, D = tax-account allocation.</summary>    
    public class PurchaseInvoiceDetailRowResponse
    {
        public string EType { get; set; } = string.Empty;
        public int Code { get; set; }
        public decimal Amount { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal TaxAmount { get; set; }
        public int GccHeadCode { get; set; }
    }
}
