using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs
{
    //public class BomItemRequest
    //{
    //    public int ProductId { get; set; }

    //    public decimal Quantity { get; set; }

    //    public int UnitId { get; set; }

    //    public string Type { get; set; } = "C";
    //}
    public class BomItemRequest
    {
        [Required(ErrorMessage = "Product is required.")]

        [Range(1, int.MaxValue,
            ErrorMessage = "Please select a valid product.")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Quantity is required.")]

        [Range(typeof(decimal), "0.000001", "999999999",
            ErrorMessage = "Quantity must be greater than zero.")]
        public decimal Quantity { get; set; }
        [Required(ErrorMessage = "UnitId is required.")]

        [Range(1, int.MaxValue,
            ErrorMessage = "Please select a valid unit.")]
        public int UnitId { get; set; }

        [Required(ErrorMessage = "Item type is required.")]
        [RegularExpression("^(C|G)$",
            ErrorMessage = "Item type must be C or G.")]
        public string? Type { get; set; } = "C";
    }

    //public class BomItemRequest
    //{
    //    //[Range(1, int.MaxValue)]
    //    public int ProductId { get; set; }

    //    //[Range(typeof(decimal), "0.000001", "999999999")]
    //    public decimal Quantity { get; set; }

    //    //[Range(1, int.MaxValue)]
    //    public int UnitId { get; set; }

    //    public string? Type { get; set; } = "C";
    //}
}