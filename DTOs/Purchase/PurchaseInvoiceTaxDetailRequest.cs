namespace QuickBalance.API.DTOs.Purchase
{
    /// <summary>One row of the tax summary (dgv_vat_details), grouped by tax code.</summary>    
    public class PurchaseInvoiceTaxDetailRequest
    {
        public int TaxCode { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }
    }
}
