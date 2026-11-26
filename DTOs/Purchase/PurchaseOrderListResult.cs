namespace QuickBalance.API.DTOs.Purchase
{
    public class PurchaseOrderListResult
    {
        public List<PurchaseOrderListResponse> Items { get; set; } = new();

        public int TotalRecords { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalPages { get; set; }
    }
    public class PurchaseOrderListResponse
    {
        public int Id { get; set; }

        public int OrderNo { get; set; }

        public string? VrType { get; set; }

        public int SupplierCode { get; set; }

        public DateTime? OrderDate { get; set; }

        public decimal Amount { get; set; }

        public string? Remarks { get; set; }
    }
}
