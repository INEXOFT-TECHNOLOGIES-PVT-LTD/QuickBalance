using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs.Sales.SalesReturn
{
    /// <summary>
    /// One returned line. Like a Sales Billing line, without the Sales Order fields.
    /// The maximum returnable quantity is checked on the server (inside the procedure).
    /// </summary>
    public class SalesReturnSaveItemRequest
    {
        // Line number (1, 2, 3...). Must be unique: serial numbers point to their line through it.
        [Range(1, int.MaxValue)]
        public int LineNo { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "ProductCode is required.")]
        public int ProductCode { get; set; }

        [Range(0, int.MaxValue)]
        public int UnitCode { get; set; }

        // 0 = godown 1 (same rule as Sales Billing).
        [Range(0, int.MaxValue)]
        public int GodownCode { get; set; }

        // "Quantity > 0" is a business rule, checked in the service.
        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PurchaseRate { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Mrp { get; set; }

        [Range(0, 100)]
        public decimal ItemDiscountPercent { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ItemDiscountAmount { get; set; }

        [Range(0, 100)]
        public decimal DiscountPercent { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DiscountAmount { get; set; }

        [Range(0, int.MaxValue)]
        public int TaxCode { get; set; }

        [Range(0, 100)]
        public decimal TaxPercent { get; set; }

        public decimal TaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }

        public string? Batch { get; set; }
        public DateTime? MfgDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        // -1 = no unit conversion (legacy convention), 0 = multiply, 1 = divide.
        [Range(-1, 1)]
        public int ConversionType { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ConversionFactor { get; set; }

        // Serial numbers being returned on this line (only when serial tracking is on).
        public List<SalesReturnSaveSerialRequest> SerialNumbers { get; set; } = new();
    }
}
