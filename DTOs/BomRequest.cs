using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs
{
    using System.ComponentModel.DataAnnotations;

    public class BomRequest : IValidatableObject
    {
        [Required(ErrorMessage = "BOM name is required.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "BOM name must be between 2 and 100 characters.")]
        public string BomName { get; set; } = string.Empty;

        [StringLength(25,
            ErrorMessage = "Alias cannot exceed 25 characters.")]
        public string? Alias { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Please select a valid product.")]
        public int PrCode { get; set; }

        [Range(typeof(decimal), "0.000001", "999999999",
            ErrorMessage = "Quantity must be greater than zero.")]
        public decimal Qty { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Please select a valid unit.")]
        public int Unit { get; set; }

        [Range(typeof(decimal), "0", "999999999",
            ErrorMessage = "Expenses cannot be negative.")]
        public decimal Expences { get; set; }

        public bool McGenFlg { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Machine generation account must be greater than zero.")]
        public int? McGen { get; set; }

        public bool McConsFlg { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Machine consumption account must be greater than zero.")]
        public int? McCons { get; set; }

        [StringLength(250,
            ErrorMessage = "Second name cannot exceed 250 characters.")]
        public string? SecondName { get; set; }

        public bool IsActive { get; set; } = true;

        public List<BomItemRequest> Items { get; set; } = [];

        public List<BomAdditionalInfoRequest> AdditionalInfo { get; set; } = [];

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            // Machine generation validation
            if (McGenFlg && (!McGen.HasValue || McGen.Value <= 0))
            {
                yield return new ValidationResult(
                    "Machine generation account is required.",
                    new[] { nameof(McGen) });
            }

            // Machine consumption validation
            if (McConsFlg && (!McCons.HasValue || McCons.Value <= 0))
            {
                yield return new ValidationResult(
                    "Machine consumption account is required.",
                    new[] { nameof(McCons) });
            }

            // Validate nested BOM items
            if (Items == null || Items.Count == 0)
            {
                yield return new ValidationResult(
                    "At least one BOM item is required.",
                    new[] { nameof(Items) });
            }

            // Validate nested additional information
            if (AdditionalInfo == null)
            {
                yield return new ValidationResult(
                    "Additional information cannot be null.",
                    new[] { nameof(AdditionalInfo) });
            }
        }
    }
    public class BomRequests
    {
        [Required]
        public string BomName { get; set; } = string.Empty;

        public string? Alias { get; set; }

        [Range(1, int.MaxValue)]
        public int PrCode { get; set; }

        //[Range(typeof(decimal), "0.000001", "999999999")]
        public decimal Qty { get; set; }

        //[Range(1, int.MaxValue)]
        public int Unit { get; set; }

        public decimal Expences { get; set; }

        public string? McGenFlg { get; set; }

        public int? McGen { get; set; }

        public string? McConsFlg { get; set; }

        public int? McCons { get; set; }

        public string? SecondName { get; set; }

        public List<BomItemRequest> Items { get; set; } = [];

        public List<BomAdditionalInfoRequest> AdditionalInfo { get; set; } = [];
    }


    public class BomMasterDto
    {
        public int Id { get; set; }
        public string? BomName { get; set; }
        public string? Alias { get; set; }
        public int PrCode { get; set; }
        public decimal Qty { get; set; }
        public int Unit { get; set; }
        public decimal Expences { get; set; }
        public bool McGenFlg { get; set; }
        public int? McGen { get; set; }
        public bool McConsFlg { get; set; }
        public int? McCons { get; set; }
        public int? LogCd { get; set; }
        public DateTime? UTime { get; set; }
        public string? SecondName { get; set; }
        public string? ProductName { get; set; }
        public string? UnitName { get; set; }
        public bool IsActive { get; set; } = true;
    }
    /// <summary>Flat row for GET /api/v1/boms (list only).</summary>
    public class BomListItemDto
    {
        public int Id { get; set; }
        public string? BomName { get; set; }
        public string? Alias { get; set; }
        public string? ProductName { get; set; }
        public decimal Qty { get; set; }
        public string? UnitName { get; set; }
        public bool IsActive { get; set; }
    }

    public class BomListDto
    {
        public int Id { get; set; }
        public string? Bom { get; set; }
        public string? Alias { get; set; }
    }

    public class BomItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public int UnitId { get; set; }
        public string? Type { get; set; }

        public decimal? Price { get; set; }
        public string? PurPriceType { get; set; }
        public decimal? AltPurPrice { get; set; }
        public int? MainUnit { get; set; }
        public int? AltUnit { get; set; }
        public string? PrName { get; set; }
        public decimal? BaseSalePrice { get; set; }
        public decimal? AltSalePrice { get; set; }
    }

    public class BomAdditionalInfoDto
    {
        public int Id { get; set; }
        public int? AccountId { get; set; }
        public decimal? Amount { get; set; }
    }

    /// <summary>Flat GET /boms/{id} response: header fields at top level plus items and additionalInfo.</summary>
    public class BomDetailsDto : BomMasterDto
    {
        public List<BomItemDto> Items { get; set; } = [];
        public List<BomAdditionalInfoDto> AdditionalInfo { get; set; } = [];
    }

    //public class BomRequest
    //{
    //    // ============================================================
    //    // BOM MASTER - BOMMAS
    //    // ============================================================

    //    public string BomName { get; set; } = "";

    //    public string SecondName { get; set; } = "";

    //    public string Alias { get; set; } = "";

    //    public int PrCode { get; set; }

    //    public decimal Qty { get; set; }

    //    public int Unit { get; set; }

    //    public decimal Expences { get; set; }

    //    public string McGenFlg { get; set; } = "N";

    //    public int McGen { get; set; }

    //    public string McConsFlg { get; set; } = "N";

    //    public int McCons { get; set; }

    //    // ============================================================
    //    // BOM ITEMS - BOMT
    //    // C = Consumed
    //    // G = Generated / By-product
    //    // ============================================================

    //    public List<BomItemRequest> Items { get; set; }
    //        = new List<BomItemRequest>();

    //    // ============================================================
    //    // ADDITIONAL INFORMATION - BOMD
    //    // ============================================================

    //    public List<BomAdditionalInfoRequest> AdditionalInfo { get; set; }
    //        = new List<BomAdditionalInfoRequest>();
    //}
}