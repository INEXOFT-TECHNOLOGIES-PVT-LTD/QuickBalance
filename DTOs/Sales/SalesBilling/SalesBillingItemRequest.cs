using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs.Sales.SalesBilling
{
    public class SalesBillingItemRequest
    {
        // SLNO - line number (1, 2, 3...). Must be unique: serial numbers
        // point to their line through this value.
        [Range(1, int.MaxValue)]
        public int LineNo { get; set; }

        // PRCODE
        [Range(1, int.MaxValue, ErrorMessage = "ProductCode is required.")]
        public int ProductCode { get; set; }

        // UNITCODE, GDWCODE. Godown 0 = godown 1 (same rule as the old API).
        [Range(0, int.MaxValue)]
        public int UnitCode { get; set; }

        [Range(0, int.MaxValue)]
        public int GodownCode { get; set; }

        // QTY, RATE. "Quantity > 0" is a business rule, checked in the service.
        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }

        // PRRATE (purchase/cost rate), MRP (0 = use Rate).
        [Range(0, double.MaxValue)]
        public decimal PurchaseRate { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Mrp { get; set; }

        // LDISCPERC, LDISCPRICE - item-wise discount.
        [Range(0, 100)]
        public decimal ItemDiscountPercent { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ItemDiscountAmount { get; set; }

        // DISCPERC, DISCPRICE - the second line discount. Its exact meaning
        // will be confirmed from the legacy Calculation() before we rely on it.
        [Range(0, 100)]
        public decimal DiscountPercent { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DiscountAmount { get; set; }

        // TAXCODE, TAXPERC, TAXABLE, TAXAMOUNT - calculated by the client for now
        // (the existing procedure needs them). NETAMOUNT is calculated by the server.
        [Range(0, int.MaxValue)]
        public int TaxCode { get; set; }

        [Range(0, 100)]
        public decimal TaxPercent { get; set; }

        public decimal TaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }

        // BATCH, MFGDATE, EXPDATE - only for batch-wise products.
        public string? Batch { get; set; }
        public DateTime? MfgDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        // --- Billing from a Sales Order (Slice 4). Leave these 0 for now. ---
        // SORDNO, ORDROWID, CONVTYPE, CONVFACT
        [Range(0, int.MaxValue)]
        public int SalesOrderNo { get; set; }

        [Range(0, int.MaxValue)]
        public int SalesOrderLineId { get; set; }

        [Range(-1, 1)]
        public int ConversionType { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ConversionFactor { get; set; }

        // @SLNODTLS rows for this line (Slice 3). Empty for non-serial products.
        public List<SalesBillingSerialRequest> SerialNumbers { get; set; } = new();
    }
}
