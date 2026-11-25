using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs.PartyGroup
{
    public sealed class PartyGroupRequest
    {
        [Required(ErrorMessage = "Group name is required.")]
        [StringLength(20)]
        public string GroupName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? GroupShortName { get; set; }

        [Required(ErrorMessage = "Group type is required.")]
        [RegularExpression(
            "^(Customer|Supplier)$",
            ErrorMessage = "Group type must be Customer or Supplier.")]
        public string GroupType { get; set; } = string.Empty;
    }
}
