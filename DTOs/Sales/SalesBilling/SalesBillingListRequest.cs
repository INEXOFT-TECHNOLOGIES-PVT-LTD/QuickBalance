namespace QuickBalance.API.DTOs.Sales.SalesBilling
{
    /// <summary>
    /// Query string for GET api/v1/sales-billing/list.
    /// The service corrects out-of-range paging values (like BrandService).
    /// </summary>
    public class SalesBillingListRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Matches the invoice number or the customer name.
        public string? Search { get; set; }

        // ToDate includes that whole day.
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // 0 = all customers.
        public int CustomerCode { get; set; }

        // invoiceNo, date, customer, amount. Anything else = newest first.
        public string? SortBy { get; set; }

        // ASC or DESC.
        public string? SortDirection { get; set; } = "DESC";
    }
}
