
namespace QuickBalance.API.DTOs.Purchase
{
    public class PurchaseOrderAdditionalDto
    {
        public int OrderNo { get; set; }

        public DateTime OrderDate { get; set; }

        public int SupplierId { get; set; }

        public int EntryType { get; set; }

        public int? Code { get; set; }

        public decimal Amount { get; set; }

        public decimal Tax { get; set; }

        public int? BType { get; set; }

        public int UserId { get; set; }

        public decimal TaxAmount { get; set; }

        public string? VrType { get; set; }

        public decimal CurrencyConversionRate { get; set; }

        public int? CurrencyId { get; set; }

        public int? DFlag { get; set; }

        public decimal OriginalAmount { get; set; }

        public decimal OriginalTaxAmount { get; set; }

        public bool CancelFlag { get; set; }

        public int? VsVrType { get; set; }
    }
}
