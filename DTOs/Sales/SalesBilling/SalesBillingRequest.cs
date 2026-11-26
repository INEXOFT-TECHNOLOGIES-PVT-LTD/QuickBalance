using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs.Sales.SalesBilling
{
    /// <summary>
    /// Body for POST api/v1/sales-billing/manage.
    /// InvoiceId = 0 creates a new invoice; InvoiceId > 0 updates that invoice.
    /// </summary>
    public class SalesBillingRequest
    {
        // @SINVNO - the INTERNAL id (SALM.INVNO), not the printed invoice number.
        [Range(0, int.MaxValue)]
        public int InvoiceId { get; set; }

        // @DATE. Nullable so that [Required] can detect a missing date.
        [Required(ErrorMessage = "InvoiceDate is required.")]
        public DateTime? InvoiceDate { get; set; }

        // @CUSCODE
        [Range(1, int.MaxValue, ErrorMessage = "CustomerCode is required.")]
        public int CustomerCode { get; set; }

        // @PAYMODE - "Cash", "Credit" or "Bank". Empty = Cash.
        // Any other value is rejected in the service (Step 4).
        public string? PayMode { get; set; }

        // @CLCODE - cash ledger. 0 = the default cash account.
        [Range(0, int.MaxValue)]
        public int CashLedgerCode { get; set; }

        // @BLCODE - bank ledger. Required when PayMode = "Bank" (checked in the service).
        [Range(0, int.MaxValue)]
        public int BankLedgerCode { get; set; }

        // @DUEDATE - mainly for Credit. Empty = InvoiceDate.
        public DateTime? DueDate { get; set; }

        // @SMCODE
        [Range(0, int.MaxValue)]
        public int SalesmanCode { get; set; }

        // @POS - sent to the procedure as "Y" / "N".
        public bool IsPos { get; set; }

        // @DISCPERC, @DISCAMT - discount on the whole bill.
        [Range(0, 100)]
        public decimal BillDiscountPercent { get; set; }

        [Range(0, double.MaxValue)]
        public decimal BillDiscountAmount { get; set; }

        // @ROUNDOFF - can be negative, so no [Range].
        public decimal RoundOff { get; set; }

        // @ITEMDTLS
        [Required]
        [MinLength(1, ErrorMessage = "At least one item is required.")]
        public List<SalesBillingItemRequest> Items { get; set; } = new();

        // @ADDIDTLS
        public List<SalesBillingChargeRequest> Charges { get; set; } = new();
    }
}
