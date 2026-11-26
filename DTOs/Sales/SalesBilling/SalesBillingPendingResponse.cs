namespace QuickBalance.API.DTOs.Sales.SalesBilling
{
    /// <summary>Response for GET api/v1/sales-billing/pending.</summary>
    public class SalesBillingPendingResponse
    {
        public SalesBillingPendingHeaderResponse Header { get; set; } = new();
        public List<SalesBillingPendingItemResponse> Items { get; set; } = new();
    }

    public class SalesBillingPendingHeaderResponse
    {
        public int InvoiceId { get; set; }            // SALM.INVNO -> send as OriginalInvoiceId when saving the return
        public int InvoiceNo { get; set; }            // SALM.ORDNO
        public string? InvoiceNoText { get; set; }
        public DateTime InvoiceDate { get; set; }
        public int CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public int SalesmanCode { get; set; }
        public string PayMode { get; set; } = string.Empty;
        public decimal BillDiscountPercent { get; set; }
        public bool IsCancelled { get; set; }
    }

    /// <summary>
    /// One invoice line that still has quantity to return.
    /// Field names match the Sales Return save request, so a line can be copied into the return form.
    /// </summary>
    public class SalesBillingPendingItemResponse
    {
        public int LineNo { get; set; }
        public int ProductCode { get; set; }
        public string? ProductName { get; set; }
        public int UnitCode { get; set; }
        public string? UnitName { get; set; }
        public int GodownCode { get; set; }

        public decimal SoldQuantity { get; set; }                   // as sold, in the line's unit
        public decimal ReturnableQuantity { get; set; }             // still returnable, in the line's unit
        public decimal ReturnableConversionQuantity { get; set; }   // still returnable, in the main unit

        public decimal Rate { get; set; }
        public decimal PurchaseRate { get; set; }
        public decimal Mrp { get; set; }
        public decimal ItemDiscountPercent { get; set; }
        public decimal ItemDiscountAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public int TaxCode { get; set; }
        public decimal TaxPercent { get; set; }

        public string? Batch { get; set; }
        public DateTime? MfgDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        public int ConversionType { get; set; }
        public decimal ConversionFactor { get; set; }
    }
}
