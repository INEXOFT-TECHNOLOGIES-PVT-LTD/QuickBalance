namespace QuickBalance.API.DTOs.Sales.SalesBilling
{
    /// <summary>
    /// Response for GET api/v1/sales-billing/get/{id}.
    /// Field names match SalesBillingRequest where possible, so the frontend
    /// can load an invoice straight into the edit form.
    /// </summary>
    public class SalesBillingDetailResponse
    {
        public SalesBillingHeaderResponse Header { get; set; } = new();
        public List<SalesBillingItemResponse> Items { get; set; } = new();
        public List<SalesBillingChargeResponse> Charges { get; set; } = new();
    }

    public class SalesBillingHeaderResponse
    {
        public int InvoiceId { get; set; }            // SALM.INVNO - internal id: use for update / cancel
        public int InvoiceNo { get; set; }            // SALM.ORDNO - the number people see
        public string? InvoiceNoText { get; set; }    // SALM.INVNOTEXT - formatted number
        public DateTime InvoiceDate { get; set; }

        public int CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public int SalesmanCode { get; set; }
        public string? SalesmanName { get; set; }

        public string PayMode { get; set; } = string.Empty;   // "Cash" / "Credit" / "Bank"
        public int CashLedgerCode { get; set; }
        public int BankLedgerCode { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsPos { get; set; }

        public decimal BillDiscountPercent { get; set; }
        public decimal BillDiscountAmount { get; set; }
        public decimal RoundOff { get; set; }

        public decimal TaxableAmount { get; set; }    // SALM.TVALUE
        public decimal TaxAmount { get; set; }        // SALM.TAX
        public decimal NetAmount { get; set; }        // SALM.NTVALUE
        public decimal Amount { get; set; }           // SALM.AMOUNT (payable)
        public decimal CashPaid { get; set; }

        public bool IsCancelled { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class SalesBillingItemResponse
    {
        public int LineNo { get; set; }
        public int ProductCode { get; set; }
        public string? ProductName { get; set; }
        public int UnitCode { get; set; }
        public string? UnitName { get; set; }
        public int GodownCode { get; set; }

        public decimal Quantity { get; set; }
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

        public int SalesOrderNo { get; set; }
        public int SalesOrderLineId { get; set; }

        public int ConversionType { get; set; }
        public decimal ConversionFactor { get; set; }
        public decimal ConversionQuantity { get; set; }

        public List<SalesBillingSerialResponse> SerialNumbers { get; set; } = new();
    }

    public class SalesBillingSerialResponse
    {
        public string? SourceVoucherType { get; set; }
        public int SourceVoucherId { get; set; }
        public int SourceVoucherNo { get; set; }
        public int SourceLineNo { get; set; }
        public string? SerialNo { get; set; }
    }

    public class SalesBillingChargeResponse
    {
        public int AccountCode { get; set; }
        public string? AccountName { get; set; }
        public decimal Amount { get; set; }
    }
}
