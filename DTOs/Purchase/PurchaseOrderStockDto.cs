
namespace QuickBalance.API.DTOs.Purchase
{
    public class PurchaseOrderStockDto
    {
        public string VrType { get; set; } = "O";

        public int ProductId { get; set; }

        public DateTime VrDate { get; set; }

        public int OrderNo { get; set; }

        public int? NtNo { get; set; }

        public int? TrnNo { get; set; }

        public decimal Quantity { get; set; }

        public string? Type { get; set; }

        public int? PartyCode { get; set; }

        public int UserId { get; set; }

        public decimal Rate { get; set; }

        public int UnitId { get; set; }

        public string? ConversionType { get; set; }

        public decimal ConversionFactor { get; set; }

        public decimal ConversionQuantity { get; set; }

        public bool ProductFlag { get; set; }

        public decimal ConversionRate { get; set; }

        public int? GodownCode { get; set; }

        public DateTime? UpdateTime { get; set; }

        public string? VsVrType { get; set; }

        public string? Prv { get; set; }

        public string? Prvn { get; set; }

        public string? Prvt { get; set; }

        public string? SntSlNo { get; set; }

        public decimal Lm { get; set; }

        public decimal AdditionalCost { get; set; }

        public decimal ConversionAdditionalCost { get; set; }

        public decimal Closes { get; set; }

        public string? OrderRowId { get; set; }

        public string? NtRowId { get; set; }

        public string? TrnRowId { get; set; }

        public int? QtnCode { get; set; }

        public string? QtnRowId { get; set; }

        public decimal? PrvQ { get; set; }

        public decimal? PrvQy { get; set; }

        public decimal? PrvY { get; set; }

        public string? PrvNy { get; set; }

        public string? PrvTy { get; set; }

        public int? NtRno { get; set; }

        public string? NtRrowId { get; set; }
    }
}
