using QuickBalance.API.DTOs.Sales.SalesBilling;

namespace QuickBalance.API.DTOs.Sales.SalesReturn
{
    /// <summary>
    /// Response for GET api/v2/sales-returns/get/{id}.
    /// Field names match SalesReturnSaveRequest where possible, so the frontend
    /// can load a return straight into the edit form.
    /// </summary>
    public class SalesReturnDetailResponse
    {
        public SalesReturnHeaderResponse Header { get; set; } = new();
        public List<SalesReturnItemResponse> Items { get; set; } = new();
        public List<SalesBillingChargeResponse> Charges { get; set; } = new();   // same shape as Sales Billing
    }

    public class SalesReturnHeaderResponse
    {
        public int ReturnId { get; set; }                   // SALM.INVNO - internal id: use for update / cancel
        public int ReturnNo { get; set; }                   // SALM.ORDNO - the number people see
        public string? ReturnNoText { get; set; }           // SALM.INVNOTEXT - formatted number
        public DateTime ReturnDate { get; set; }

        public int OriginalInvoiceId { get; set; }          // INVNO of the invoice (send this back on update); 0 = without invoice
        public int OriginalInvoiceNo { get; set; }          // SALM.RTNINVNO - the invoice number people see
        public string? OriginalInvoiceNoText { get; set; }

        public int CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public int SalesmanCode { get; set; }
        public string? SalesmanName { get; set; }

        public string PayMode { get; set; } = string.Empty; // "Cash" / "Credit" / "Bank"
        public int CashLedgerCode { get; set; }
        public int BankLedgerCode { get; set; }
        public DateTime? DueDate { get; set; }

        public decimal BillDiscountPercent { get; set; }
        public decimal BillDiscountAmount { get; set; }
        public decimal RoundOff { get; set; }

        public decimal TaxableAmount { get; set; }          // SALM.TVALUE
        public decimal TaxAmount { get; set; }              // SALM.TAX
        public decimal NetAmount { get; set; }              // SALM.NTVALUE
        public decimal Amount { get; set; }                 // SALM.AMOUNT
        public decimal CashPaid { get; set; }

        public bool IsCancelled { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class SalesReturnItemResponse
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

        public int ConversionType { get; set; }
        public decimal ConversionFactor { get; set; }
        public decimal ConversionQuantity { get; set; }

        public List<SalesReturnSerialResponse> SerialNumbers { get; set; } = new();
    }

    public class SalesReturnSerialResponse
    {
        public string? SerialNo { get; set; }
    }
}
