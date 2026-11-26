using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs
{
    public class UnitMasterRequest
    {
        [Required(ErrorMessage = "Unit name is required.")]
        [StringLength(25, ErrorMessage = "Unit name cannot exceed 50 characters.")]
        public string UnitName { get; set; } = "";

        [StringLength(25, ErrorMessage = "Second name cannot exceed 50 characters.")]
        public string? SecondName { get; set; } = "";

        [StringLength(25, ErrorMessage = "Formal name cannot exceed 50 characters.")]
        public string? FormalName { get; set; } = "";

        public bool IsActive { get; set; } = true;
    }


    public sealed class UnitMasterResponse
    {
        public int Id { get; set; }

        public string UnitName { get; set; } = string.Empty;

        public string SecondName { get; set; } = string.Empty;

        public string FormalName { get; set; } = string.Empty;

        // Preserves the legacy API response: "YES" or "NO".
        public bool IsActive { get; set; } = false;
    }

    public sealed class UnitDropdownResponse
    {
        public int UnitId { get; set; }

        public string UnitName { get; set; } = string.Empty;
    }
}