namespace QuickBalance.API.DTOs.Sales.SalesBilling
{
    /// <summary>
    /// Query for GET api/v1/sales-billing/pending: which lines of a sales invoice can still be returned.
    /// Give InvoiceId (SALM.INVNO) OR InvoiceNoText (the printed number). InvoiceId wins when both are given.
    /// </summary>
    public class SalesBillingPendingRequest
    {
        public int InvoiceId { get; set; }

        public string? InvoiceNoText { get; set; }

        // When EDITING a return: that return's id (SALM.INVNO), so its own lines are not counted as already returned.
        public int ReturnId { get; set; }

        // Optional: when > 0, an invoice of another customer is refused.
        public int CustomerCode { get; set; }
    }
}
