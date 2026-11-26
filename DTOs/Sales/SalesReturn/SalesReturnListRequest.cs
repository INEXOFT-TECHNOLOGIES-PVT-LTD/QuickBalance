namespace QuickBalance.API.DTOs.Sales.SalesReturn
{
    /// <summary>Query string for GET api/v2/sales-returns/list.</summary>
    public class SalesReturnListRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Matches return number text, return number, customer name or original invoice number text.
        public string? Search { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // 0 = all customers.
        public int CustomerCode { get; set; }

        // returnNo / date / customer / amount (anything else = newest first).
        public string? SortBy { get; set; }

        // ASC or DESC.
        public string? SortDirection { get; set; } = "DESC";
    }
}
