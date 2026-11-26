using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs.Currencies
{
    //public class CurrencyRequest
    //{
    //    public string? Symbol { get; set; }

    //    public string? CurrString { get; set; }

    //    public string? SubString { get; set; }

    //    public int? ConvMode { get; set; }

    //    public bool? BaseFlg { get; set; }

    //    public string? SecSymbol { get; set; }

    //    public string? SecCurrString { get; set; }

    //    public string? SecSubString { get; set; }
    //}


    public class CurrencyRequest
    {
        [StringLength(50, ErrorMessage = "Symbol cannot exceed 50 characters.")]
        public string? Symbol { get; set; }

        [StringLength(50, ErrorMessage = "Currency name cannot exceed 50 characters.")]
        public string? CurrString { get; set; }

        [StringLength(50, ErrorMessage = "Sub currency name cannot exceed 50 characters.")]
        public string? SubString { get; set; }

        public int? ConvMode { get; set; }

        public bool? BaseFlg { get; set; }

        [StringLength(50, ErrorMessage = "Secondary symbol cannot exceed 50 characters.")]
        public string? SecSymbol { get; set; }

        [StringLength(50, ErrorMessage = "Secondary currency name cannot exceed 50 characters.")]
        public string? SecCurrString { get; set; }

        [StringLength(50, ErrorMessage = "Secondary sub currency name cannot exceed 50 characters.")]
        public string? SecSubString { get; set; }
    }
}
