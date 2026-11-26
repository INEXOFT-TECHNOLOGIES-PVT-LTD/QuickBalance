using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs
{


    //public class CountryDtos
    //{
    //    public int CountryId { get; set; }
    //    public string CountryName { get; set; } = string.Empty;
    //    public decimal? VatNoLimit { get; set; }
    //    public string? VatCharAllow { get; set; }
    //}

    public class CountryDtos
    {
        public int Id { get; set; }
        public string? CountryName { get; set; }
        public bool? Active { get; set; }
        public string? CurSymbol { get; set; }
        public string? Farabic { get; set; }
        public string? FormalName { get; set; }
        public string? Sarabic { get; set; }
        public string? CurName { get; set; }
        public string? Tarabic { get; set; }
        public bool? Vat { get; set; }
        public decimal? VatPer { get; set; }
        public string? Lng { get; set; }
        public string? VatLabel { get; set; }
        public int? VatNoLimit { get; set; }
        public string? Iso { get; set; }
        public string? Iso3 { get; set; }
        public int? NumCode { get; set; }
        public int? PhCode { get; set; }
        public string? CountrySecName { get; set; }
        //public bool? VatCharAllow { get; set; }
    }


    public class CountryRequest
    {
        [Required(ErrorMessage = "Country name is required.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Country name must be between 2 and 100 characters.")]
        public string CountryName { get; set; } = string.Empty;


        [StringLength(250,
            ErrorMessage = "Currency symbol cannot exceed 250 characters.")]
        public string? CurSymbol { get; set; }


        [StringLength(250,
            ErrorMessage = "Arabic country name cannot exceed 250 characters.")]
        public string? Farabic { get; set; }


        [StringLength(250,
            ErrorMessage = "Formal name cannot exceed 250 characters.")]
        public string? FormalName { get; set; }


        [StringLength(250,
            ErrorMessage = "Arabic secondary country name cannot exceed 250 characters.")]
        public string? Sarabic { get; set; }


        [StringLength(250,
            ErrorMessage = "Currency name cannot exceed 250 characters.")]
        public string? CurName { get; set; }


        [StringLength(250,
            ErrorMessage = "Arabic tertiary country name cannot exceed 250 characters.")]
        public string? Tarabic { get; set; }


        public bool? Vat { get; set; }


        [Range(typeof(decimal), "0", "9999999999999999.99",
            ErrorMessage = "VAT percentage must be zero or greater.")]
        public decimal? VatPer { get; set; }


        [StringLength(250,
            ErrorMessage = "Language cannot exceed 250 characters.")]
        public string? Lng { get; set; }


        [StringLength(250,
            ErrorMessage = "VAT label cannot exceed 250 characters.")]
        public string? VatLabel { get; set; }


        [Range(0, int.MaxValue,
            ErrorMessage = "VAT number limit cannot be negative.")]
        public int? VatNoLimit { get; set; }


        [StringLength(10,
            ErrorMessage = "ISO code cannot exceed 10 characters.")]
        public string? Iso { get; set; }


        [StringLength(10,
            ErrorMessage = "ISO3 code cannot exceed 10 characters.")]
        public string? Iso3 { get; set; }


        [Range(0, int.MaxValue,
            ErrorMessage = "Numeric country code cannot be negative.")]
        public int? NumCode { get; set; }


        [Range(0, int.MaxValue,
            ErrorMessage = "Phone code cannot be negative.")]
        public int? PhCode { get; set; }


        [StringLength(250,
            ErrorMessage = "Secondary country name cannot exceed 250 characters.")]
        public string? CountrySecName { get; set; }


        public bool? VatCharAllow { get; set; }
    }
}
