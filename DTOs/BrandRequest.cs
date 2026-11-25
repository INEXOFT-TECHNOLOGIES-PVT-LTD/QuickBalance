using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs
{
    public class BrandRequest
    {
        [Required(ErrorMessage = "Brand name is required.")]
        [StringLength(50, MinimumLength = 2,
         ErrorMessage = "Brand name must be between 2 and 50 characters.")]
        public string BrandName { get; set; } = "";

        [StringLength(100,
            ErrorMessage = "Second name cannot exceed 100 characters.")]
        public string? SecondName { get; set; } = "";

        [StringLength(25,
            ErrorMessage = "Alias cannot exceed 25 characters.")]
        public string? Alias { get; set; } = "";
    }
}