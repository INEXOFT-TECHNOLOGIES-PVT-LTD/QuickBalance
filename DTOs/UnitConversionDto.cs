using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs
{
    public class UnitConversionDto
    {
        public int Id { get; set; }
        public int? MainUnit { get; set; }
        public int? SubUnit { get; set; }
        public decimal? ConvFact { get; set; }
        public string? MainUnitName { get; set; }
        public string? SubUnitName { get; set; }
        //public int? LogCd { get; set; }
        //public DateTime? UTime { get; set; }
    }


    public class UnitConversionRequestDto
    {
        [Required(ErrorMessage = "Main unit is required.")]
        [Range(1, int.MaxValue,
             ErrorMessage = "Main unit must be greater than zero.")]
        public int? MainUnit { get; set; }

        [Required(ErrorMessage = "Sub unit is required.")]
        [Range(1, int.MaxValue,
            ErrorMessage = "Sub unit must be greater than zero.")]
        public int? SubUnit { get; set; }

        [Required(ErrorMessage = "Conversion factor is required.")]
        [Range(typeof(decimal), "0.000001", "999999999999",
            ErrorMessage = "Conversion factor must be greater than zero.")]
        public decimal? ConvFact { get; set; }

    }
}
