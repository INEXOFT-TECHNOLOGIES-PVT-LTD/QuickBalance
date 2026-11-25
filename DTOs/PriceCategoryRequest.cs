using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs
{
    public class PriceCategoryLevelInputRequest
    {
        /// <summary>Optional level id echoed back on edit. Ignored: saving replaces all levels.</summary>
        public int? Id { get; set; }

        [Required(ErrorMessage = "Discount structure is required.")]
        [StringLength(
            25,
            MinimumLength = 1,
            ErrorMessage = "Discount structure must be between 1 and 25 characters.")]
        public string DiscountStructure { get; set; } = "";

        [Range(
            typeof(decimal),
            "0",
            "100000",
            ErrorMessage = "Level from must be between 0 and 100000.")]
        public decimal LevelFrom { get; set; }

        [Range(
            typeof(decimal),
            "0",
            "100000",
            ErrorMessage = "Level to must be between 0 and 100000.")]
        public decimal LevelTo { get; set; }

        [Range(
            typeof(decimal),
            "0",
            "100",
            ErrorMessage = "Discount percentage must be between 0 and 100.")]
        public decimal DiscountPercent { get; set; }

        [Range(
            typeof(decimal),
            "0",
            "100000",
            ErrorMessage = "Effective value must be between 0 and 100000.")]
        public decimal EffectiveValue { get; set; }

        [Range(
            typeof(decimal),
            "0",
            "100000",
            ErrorMessage = "Applied price must be between 0 and 100000.")]
        public decimal AppliedPrice { get; set; }
    }

    public class PriceCategoryRequest
    {
        [Required(ErrorMessage = "Price category name is required.")]
        [StringLength(
            25,
            MinimumLength = 2,
            ErrorMessage = "Price category name must be between 2 and 100 characters.")]
        public string CategoryName { get; set; } = "";

        [Required(ErrorMessage = "At least one price category level is required.")]
        [MinLength(
            1,
            ErrorMessage = "At least one price category level is required.")]
        public List<PriceCategoryLevelInputRequest> Levels { get; set; } = new();
    }
}