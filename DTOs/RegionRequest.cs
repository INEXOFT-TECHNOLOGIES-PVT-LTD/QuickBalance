using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs
{
    //public class RegionRequest
    //{
    //    public int CountryId { get; set; }
    //    public string? RegionName { get; set; }
    //    public string? RegionSecName { get; set; }
    //    public bool IsActive { get; set; }


    //}

    public class RegionRequest
    {
        [Required(ErrorMessage = "Country ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Valid country ID is required.")]
        public int CountryId { get; set; }

        [Required(ErrorMessage = "Region name is required.")]
        [StringLength(50, ErrorMessage = "Region name cannot exceed 50 characters.")]
        public string? RegionName { get; set; }

        [StringLength(50, ErrorMessage = "Secondary region name cannot exceed 50 characters.")]
        public string? RegionSecName { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
