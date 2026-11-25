using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs.ItemCategory
{
    public class ItemCategoryRequest
    {
        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Alias { get; set; }

        [StringLength(100)]
        public string? SecondName { get; set; }

        public bool IsActive { get; set; } = true;
    }


    public class ItemCategoryResponse
    {
        public int Id { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Alias { get; set; }
        public string? SecondName { get; set; }
        public bool IsActive { get; set; }
        //public int? UserId { get; set; }
        //public DateTime? UpdatedAt { get; set; }
    }
}