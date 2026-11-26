using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs.Purchase
{
    /// <summary>    
    /// One grid row. Unit conversion, tax code, tax %, and tax account are taken from    
    /// PRMAST / TAXMAS on the server (review §12.4), not from the client.    
    /// </summary>    
    public class PurchaseInvoiceItemRequest
    {
        public int SlNo { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Invalid Product choosen")]
        public int ProductCode { get; set; }

        /// <summary>Must be PRMAST.UNIT (main) or PRMAST.ALTUNIT.</summary>    
        [Range(1, int.MaxValue, ErrorMessage = "Invalid Unit choosen")]
        public int UnitCode { get; set; }

        public decimal Qty { get; set; }

        /// <summary>Price per selected unit, in the document currency.</summary>    
        public decimal Price { get; set; }

        public decimal Mrp { get; set; }

        /// <summary>Line discount %. Takes precedence over LineDiscountValue.</summary>    
        public decimal? LineDiscountPercentage { get; set; }

        /// <summary>Line discount amount per unit.</summary>    
        public decimal? LineDiscountValue { get; set; }

        /// <summary>Invoice-level discount % for this line. Null = use the header discount (additional row 1).</summary>    
        public decimal? DiscountPercentage { get; set; }

        /// <summary>0 = default godown (1, as GetGodownByBranch without VRSYS/branch).</summary>    
        public int GodownCode { get; set; }

        [MaxLength(100)]
        public string? Batch { get; set; }

        /// <summary>Defaults to the purchase date (claim 20).</summary>    
        public DateTime? MfgDate { get; set; }

        /// <summary>Defaults to the purchase date (claim 20).</summary>    
        public DateTime? ExpiryDate { get; set; }
    }
}
