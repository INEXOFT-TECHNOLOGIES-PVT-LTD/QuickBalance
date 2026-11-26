namespace QuickBalance.API.DTOs.Sales.SalesBilling
{
    /// <summary>
    /// One row of GET api/v1/sales-billing/list.
    /// </summary>
    public class SalesBillingListItemResponse
    {
        public int InvoiceId { get; set; }            // internal id: use for get / update / cancel
        public int InvoiceNo { get; set; }            // the number people see
        public string? InvoiceNoText { get; set; }
        public DateTime InvoiceDate { get; set; }

        public int CustomerCode { get; set; }
        public string? CustomerName { get; set; }

        public string PayMode { get; set; } = string.Empty;   // "Cash" / "Credit" / "Bank"

        public decimal TaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Amount { get; set; }

        public bool IsCancelled { get; set; }
    }
}
