
namespace QuickBalance.API.DTOs.Purchase
{
    public class PurchaseOrderMasterDto
    {
        public DateTime OrderDate { get; set; }

        public int SupplierId { get; set; }

        public decimal DiscountPercentage { get; set; }

        public decimal DiscountValue { get; set; }

        public string? Remarks { get; set; }

        public decimal Freight { get; set; }

        public int? InvoiceFormatId { get; set; }

        public string? InvoiceNumber { get; set; }

        public DateTime? InvoiceDate { get; set; }

        public decimal CurrencyConversionRate { get; set; } = 1;

        public int? CurrencyId { get; set; }
    }
}
