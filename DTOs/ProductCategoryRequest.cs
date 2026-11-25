namespace QuickBalance.API.DTOs
{
    using System.ComponentModel.DataAnnotations;

    public sealed class ItemGroupRequest : IValidatableObject
    {
        [Required(ErrorMessage = "Group name is required.")]
        [StringLength(50, MinimumLength = 2,
            ErrorMessage = "Group name must be between 2 and 50 characters.")]
        public string GroupName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Group code is required.")]
        [Range(1, int.MaxValue,
            ErrorMessage = "Group code must be greater than zero.")]
        public int? GroupCode { get; set; }

        public bool IsPrimaryGroup { get; set; }

        [Range(0, int.MaxValue,
            ErrorMessage = "Primary group ID cannot be negative.")]
        public int? PrimaryGroupId { get; set; }

        public bool TagWithAccount { get; set; }

        [Range(0, int.MaxValue,
            ErrorMessage = "Stock account ID cannot be negative.")]
        public int? StockAccountId { get; set; }

        [Range(0, int.MaxValue,
            ErrorMessage = "Purchase account ID cannot be negative.")]
        public int? PurchaseAccountId { get; set; }

        [Range(0, int.MaxValue,
            ErrorMessage = "Sales account ID cannot be negative.")]
        public int? SalesAccountId { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(250,
            ErrorMessage = "Secondary name cannot exceed 250 characters.")]
        public string? SecName { get; set; }

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (!IsPrimaryGroup && (PrimaryGroupId ?? 0) <= 0)
            {
                yield return new ValidationResult(
                    "Please select a valid primary group.",
                    new[] { nameof(PrimaryGroupId) });
            }

            if (TagWithAccount)
            {
                if ((StockAccountId ?? 0) <= 0)
                {
                    yield return new ValidationResult(
                        "Please select a valid stock account.",
                        new[] { nameof(StockAccountId) });
                }

                if ((PurchaseAccountId ?? 0) <= 0)
                {
                    yield return new ValidationResult(
                        "Please select a valid purchase account.",
                        new[] { nameof(PurchaseAccountId) });
                }

                if ((SalesAccountId ?? 0) <= 0)
                {
                    yield return new ValidationResult(
                        "Please select a valid sales account.",
                        new[] { nameof(SalesAccountId) });
                }
            }
        }
    }

    //public sealed class ItemGroupRequest
    //{
    //    public string GroupName { get; set; } = string.Empty;
    //    public int GroupCode { get; set; }

    //    public bool IsPrimaryGroup { get; set; }
    //    public int PrimaryGroupId { get; set; }

    //    public bool TagWithAccount { get; set; }
    //    public int StockAccount { get; set; }
    //    public int PurchaseAccount { get; set; }
    //    public int SalesAccount { get; set; }

    //    public bool IsActive { get; set; } = true;
    //    public string? SecName { get; set; }
    //}
}
