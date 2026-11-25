using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace QuickBalance.API.DTOs
{
    public class BulkProductRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one product is required.")]
        [MaxLength(500, ErrorMessage = "Maximum 500 products per request.")]
        public List<ProductRequest> Products { get; set; } = new();
    }
}
