using System.ComponentModel.DataAnnotations;
using QuickBalance.API.DTOs.Sales.SalesBilling;

namespace QuickBalance.API.DTOs.Sales.SalesReturn
{
    /// <summary>
    /// Body for the NEW sales return "manage" endpoint.
    /// ReturnId = 0 creates a new sales return; ReturnId > 0 updates that return.
    /// (The old DTOs/SalesReturnRequest.cs belongs to the old controller and is not used here.)
    /// </summary>
    public class SalesReturnSaveRequest
    {
        // The return's INTERNAL id (SALM.INVNO of the return), not its printed number.
        [Range(0, int.MaxValue)]
        public int ReturnId { get; set; }

        // The INTERNAL id (SALM.INVNO) of the sales invoice being returned against,
        // the same value as InvoiceId in Sales Billing.
        // 0 = return without invoice (only allowed when the company setting SRWITHOUTINV is on).
        [Range(0, int.MaxValue)]
        public int OriginalInvoiceId { get; set; }

        [Required(ErrorMessage = "ReturnDate is required.")]
        public DateTime? ReturnDate { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "CustomerCode is required.")]
        public int CustomerCode { get; set; }

        // "Cash", "Credit" or "Bank". Empty = Cash. Other values are rejected in the service.
        public string? PayMode { get; set; }

        // 0 = the default cash account.
        [Range(0, int.MaxValue)]
        public int CashLedgerCode { get; set; }

        // Required when PayMode = "Bank" (checked in the service).
        [Range(0, int.MaxValue)]
        public int BankLedgerCode { get; set; }

        // Empty = ReturnDate.
        public DateTime? DueDate { get; set; }

        [Range(0, int.MaxValue)]
        public int SalesmanCode { get; set; }

        [Range(0, 100)]
        public decimal BillDiscountPercent { get; set; }

        [Range(0, double.MaxValue)]
        public decimal BillDiscountAmount { get; set; }

        // Can be negative, so no [Range].
        public decimal RoundOff { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one item is required.")]
        public List<SalesReturnSaveItemRequest> Items { get; set; } = new();

        // Same shape and meaning as Sales Billing charges (account + amount).
        public List<SalesBillingChargeRequest> Charges { get; set; } = new();
    }
}
