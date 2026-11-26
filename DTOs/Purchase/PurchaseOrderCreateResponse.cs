namespace QuickBalance.API.DTOs.Purchase
{
    public class PurchaseOrderCreateResponse
    {
        public int Id { get; set; }

        public int OrderNo { get; set; }

        public string? OrderNumber { get; set; }

        public DateTime OrderDate { get; set; }

        public int SupplierId { get; set; }

        public decimal SubTotal { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal Freight { get; set; }

        public decimal NetAmount { get; set; }

        public string? Status { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}
