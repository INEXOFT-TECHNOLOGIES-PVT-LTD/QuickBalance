namespace QuickBalance.API.DTOs.Sales.SalesReturn
{
    /// <summary>One row of GET api/v2/sales-returns/list.</summary>
    public class SalesReturnListItemResponse
    {
        public int ReturnId { get; set; }
        public int ReturnNo { get; set; }
        public string? ReturnNoText { get; set; }
        public DateTime ReturnDate { get; set; }

        public int OriginalInvoiceNo { get; set; }
        public string? OriginalInvoiceNoText { get; set; }

        public int CustomerCode { get; set; }
        public string? CustomerName { get; set; }

        public string PayMode { get; set; } = string.Empty;
        public decimal TaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Amount { get; set; }
        public bool IsCancelled { get; set; }
    }
}
