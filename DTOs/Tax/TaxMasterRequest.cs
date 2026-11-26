using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs.Tax
{
    /// <summary>
    /// Create / update request for a tax (dbo.TAXMAS).
    /// All six account IDs are required: legacy reports (Day Book, P&amp;L)
    /// INNER JOIN TAXMAS to ACCMAS on each of them.
    /// </summary>
    public sealed class TaxMasterRequest
    {
        [Required(ErrorMessage = "Tax name is required.")]
        [StringLength(100)]
        public string TaxName { get; set; } = string.Empty;

        [Range(0, 100, ErrorMessage = "Tax percentage must be between 0 and 100.")]
        public decimal TaxPercentage { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>ACCMAS.ID of the purchase ledger (TAXMAS.PURCODE).</summary>
        [Required(ErrorMessage = "Purchase account is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Purchase account is required.")]
        public int? PurchaseAccountId { get; set; }

        /// <summary>ACCMAS.ID of the purchase return ledger (TAXMAS.PRTNCODE).</summary>
        [Required(ErrorMessage = "Purchase return account is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Purchase return account is required.")]
        public int? PurchaseReturnAccountId { get; set; }

        /// <summary>ACCMAS.ID of the sales ledger (TAXMAS.SALCODE).</summary>
        [Required(ErrorMessage = "Sales account is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Sales account is required.")]
        public int? SalesAccountId { get; set; }

        /// <summary>ACCMAS.ID of the sales return ledger (TAXMAS.SRTNCODE).</summary>
        [Required(ErrorMessage = "Sales return account is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Sales return account is required.")]
        public int? SalesReturnAccountId { get; set; }

        /// <summary>ACCMAS.ID of the output VAT ledger used on sales (TAXMAS.VATCODE).</summary>
        [Required(ErrorMessage = "VAT account is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "VAT account is required.")]
        public int? VatAccountId { get; set; }

        /// <summary>ACCMAS.ID of the input tax ledger used on purchases (TAXMAS.PTAXCODE).</summary>
        [Required(ErrorMessage = "Tax account is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Tax account is required.")]
        public int? TaxAccountId { get; set; }

        [StringLength(100)]
        public string? SecondName { get; set; }

        /// <summary>
        /// Tax type (TAXMAS.TTYPE). Optional, defaults to "T".
        /// T = Taxable, E = Exempted, UR = Unregistered.
        /// Item Master tax lookups exclude E and UR.
        /// </summary>
        [StringLength(10)]
        public string? TaxType { get; set; }
    }
}
