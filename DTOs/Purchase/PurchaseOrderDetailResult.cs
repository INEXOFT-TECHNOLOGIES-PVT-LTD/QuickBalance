namespace QuickBalance.API.DTOs.Purchase
{
    public class PurchaseOrderDetailResult
    {
        public int Id { get; set; }
        public int OrderNo { get; set; }
        public string? VrType { get; set; }
        public DateTime? OrderDate { get; set; }
        public int SupplierCode { get; set; }

        public decimal DiscountPercentage { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal Tax { get; set; }
        public decimal NetValue { get; set; }
        public decimal TotalValue { get; set; }
        public decimal Amount { get; set; }
        public decimal Freight { get; set; }

        public string? Remarks { get; set; }

        public List<dynamic> Items { get; set; } = new();
    }
}
