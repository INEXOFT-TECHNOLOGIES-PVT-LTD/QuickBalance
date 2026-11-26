
namespace QuickBalance.API.DTOs.Purchase
{
    public class PurchaseOrderItemDto
    {
        public int ProductId { get; set; }

        public decimal Quantity { get; set; }

        public int UnitId { get; set; }

        public decimal Rate { get; set; }

        public int? TaxCode { get; set; }

        public decimal Discount { get; set; }

        public int? GodownId { get; set; }

        public decimal ConversionFactor { get; set; } = 1;

        public decimal ConversionQuantity { get; set; }
    }
}
